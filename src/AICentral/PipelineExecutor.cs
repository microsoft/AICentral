using AICentral.Core;
using AICentral.EndpointSelectors;
using AICentral.EndpointSelectors.ResultHandlers;
using Microsoft.Extensions.Primitives;

namespace AICentral;

public class PipelineExecutor : IAICentralPipelineExecutor, IAICentralResponseGenerator, IDisposable
{
    private readonly IAICentralEndpointSelector _iaiCentralEndpointSelector;
    private readonly IEnumerator<IAICentralPipelineStep> _pipelineEnumerator;
    private readonly IList<IAICentralPipelineStep> _outputHandlers = new List<IAICentralPipelineStep>();

    public PipelineExecutor(
        IEnumerable<IAICentralPipelineStep> steps,
        IAICentralEndpointSelector iaiCentralEndpointSelector)
    {
        _iaiCentralEndpointSelector = iaiCentralEndpointSelector;
        _pipelineEnumerator = steps.GetEnumerator();
    }

    public Task<AICentralResponse> Next(HttpContext context, IncomingCallDetails requestDetails,
        CancellationToken cancellationToken)
    {

        if (_pipelineEnumerator.MoveNext())
        {
            _outputHandlers.Add(_pipelineEnumerator.Current);
            return _pipelineEnumerator.Current.Handle(context, requestDetails, this, cancellationToken);
        }

        return _iaiCentralEndpointSelector.Handle(context, requestDetails, true, this, cancellationToken);
    }

    public void Dispose()
    {
        _pipelineEnumerator.Dispose();
    }

    /// <summary>
    /// Give everything a chance to change / add headers. The main reason is the token limits that are sent back from Open AI.
    /// They become a bit meaningless when you are load balancing across multiple servers.
    /// </summary>
    /// <param name="requestInformation"></param>
    /// <param name="context"></param>
    /// <param name="rawResponse"></param>
    /// <param name="responseMetadata"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    async Task<AICentralResponse> IAICentralResponseGenerator.BuildResponse(
        DownstreamRequestInformation requestInformation, HttpContext context,
        HttpResponseMessage rawResponse, 
        ResponseMetadata responseMetadata,
        CancellationToken cancellationToken)
    {
        await _iaiCentralEndpointSelector.BuildResponseHeaders(context, rawResponse, responseMetadata.SanitisedHeaders);
        
        foreach (var completedStep in _outputHandlers)
        {
            await completedStep.BuildResponseHeaders(context, rawResponse, responseMetadata.SanitisedHeaders);
        }

        return await HandleResponse(requestInformation, context, rawResponse, responseMetadata.SanitisedHeaders, cancellationToken);
    }

    private Task<AICentralResponse> HandleResponse(
        DownstreamRequestInformation requestInformation,
        HttpContext context, 
        HttpResponseMessage openAiResponse,
        Dictionary<string, StringValues> sanitisedResponseHeaders, 
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<PipelineExecutor>>();

        //decision point... If this is a streaming request, then we should start streaming the result now.
        logger.LogDebug("Received Azure Open AI Response. Status Code: {StatusCode}", openAiResponse.StatusCode);
        
        CopyHeadersToResponse(context.Response, sanitisedResponseHeaders);

        if (openAiResponse.Headers.TransferEncodingChunked == true)
        {
            logger.LogDebug("Detected chunked encoding response. Streaming response back to consumer");
            return ServerSideEventResponseHandler.Handle(
                context,
                cancellationToken,
                openAiResponse,
                requestInformation);
        }

        if ((openAiResponse.Content.Headers.ContentType?.MediaType ?? string.Empty).Contains("json", StringComparison.InvariantCultureIgnoreCase))
        {
            logger.LogDebug("Detected non-chunked encoding response. Sending response back to consumer");
            return JsonResponseHandler.Handle(
                context,
                cancellationToken,
                openAiResponse,
                requestInformation);
        }

        return StreamResponseHandler.Handle(
            context,
            cancellationToken,
            openAiResponse,
            requestInformation);
    }
    
    private static void CopyHeadersToResponse(HttpResponse response, Dictionary<string, StringValues> headersToProxy)
    {
        foreach (var header in headersToProxy)
        {
            response.Headers.TryAdd(header.Key, header.Value);
        }
    }

}