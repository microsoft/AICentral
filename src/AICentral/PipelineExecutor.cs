using AICentral.Core;
using AICentral.ResultHandlers;
using Microsoft.Extensions.Primitives;

namespace AICentral;

public class PipelineExecutor : IResponseGenerator, IDisposable
{
    private readonly Func<IncomingCallDetails, IEndpointSelector> _endpointSelectorChooser;
    private readonly IEnumerator<IPipelineStep> _pipelineEnumerator;
    private readonly IList<IPipelineStep> _outputHandlers = new List<IPipelineStep>();
    private IEndpointSelector? _endpointSelector;

    public PipelineExecutor(
        IEnumerable<IPipelineStep> steps,
        Func<IncomingCallDetails, IEndpointSelector> endpointSelectorChooser)
    {
        _endpointSelectorChooser = endpointSelectorChooser;
        _pipelineEnumerator = steps.GetEnumerator();
    }

    public Task<AICentralResponse> Next(IRequestContext context, IncomingCallDetails requestDetails,
        CancellationToken cancellationToken)
    {

        if (_pipelineEnumerator.MoveNext())
        {
            _outputHandlers.Add(_pipelineEnumerator.Current);
            return _pipelineEnumerator.Current.Handle(context, requestDetails, Next, cancellationToken);
        }

        //Once we have enumerated the steps, work out which endpoint selector is going to handle the request.
        //If a step detects affinity then we need to honour it.
        _endpointSelector = _endpointSelectorChooser(requestDetails);
        
        return _endpointSelector.Handle(context, requestDetails, true, this, cancellationToken);
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
    async Task<AICentralResponse> IResponseGenerator.BuildResponse(
        DownstreamRequestInformation requestInformation, 
        IRequestContext context,
        HttpResponseMessage rawResponse, 
        ResponseMetadata responseMetadata,
        CancellationToken cancellationToken)
    {
        await _endpointSelector!.BuildResponseHeaders(context, rawResponse, responseMetadata.SanitisedHeaders);
        
        foreach (var completedStep in _outputHandlers)
        {
            await completedStep.BuildResponseHeaders(context, rawResponse, responseMetadata.SanitisedHeaders);
        }

        return await HandleResponse(requestInformation, context, rawResponse, responseMetadata, cancellationToken);
    }

    private Task<AICentralResponse> HandleResponse(
        DownstreamRequestInformation requestInformation,
        IRequestContext context, 
        HttpResponseMessage openAiResponse,
        ResponseMetadata responseMetadata, 
        CancellationToken cancellationToken)
    {
        var logger = context.GetLogger<PipelineExecutor>();

        //decision point... If this is a streaming request, then we should start streaming the result now.
        logger.LogDebug("Received Azure Open AI Response. Status Code: {StatusCode}", openAiResponse.StatusCode);
        
        CopyHeadersToResponse(context.ResponseHeaders, responseMetadata.SanitisedHeaders);

        if (openAiResponse.Content.Headers.ContentType?.MediaType?.Equals("text/event-stream") ?? false)
        {
            logger.LogDebug("Detected SSE response. Streaming response back to consumer");
            return context.CreateServerSideEventResponseHandler().Handle(
                context,
                cancellationToken,
                openAiResponse,
                requestInformation,
                responseMetadata);
        }

        if ((openAiResponse.Content.Headers.ContentType?.MediaType ?? string.Empty).Contains("json", StringComparison.InvariantCultureIgnoreCase))
        {
            logger.LogDebug("Detected non-chunked encoding response. Sending response back to consumer");
            return context.CreateJsonResponseHandler().Handle(
                context,
                cancellationToken,
                openAiResponse,
                requestInformation,
                responseMetadata);
        }

        return context.CreateStreamResponseHandler().Handle(
            context,
            cancellationToken,
            openAiResponse,
            requestInformation,
            responseMetadata);
    }
    
    private static void CopyHeadersToResponse(IHeaderDictionary responseHeaders, Dictionary<string, StringValues> headersToProxy)
    {
        foreach (var header in headersToProxy)
        {
            responseHeaders.TryAdd(header.Key, header.Value);
        }
    }

}