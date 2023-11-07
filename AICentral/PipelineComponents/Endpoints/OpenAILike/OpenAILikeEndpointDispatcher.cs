using System.Diagnostics;
using System.Net;
using AICentral.PipelineComponents.Endpoints.OpenAILike.OpenAI;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Http;

namespace AICentral.PipelineComponents.Endpoints.OpenAILike;

public abstract class OpenAILikeEndpointDispatcher : IAICentralEndpointDispatcher
{
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _id;

    public OpenAILikeEndpointDispatcher(
        string id,
        Dictionary<string, string> modelMappings)
    {
        _id = id;
        _modelMappings = modelMappings;
    }

    public async Task<(AICentralRequestInformation, HttpResponseMessage)> Handle(
        HttpContext context,
        AICallInformation callInformation,
        AICentralPipelineExecutor pipeline, 
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<OpenAIEndpointDispatcherBuilder>>();

        var mappedModelName = _modelMappings.TryGetValue(callInformation.IncomingModelName ?? string.Empty, out var mapping)
            ? mapping
            : string.Empty;

        if (mappedModelName == string.Empty)
        {
            return (
                new AICentralRequestInformation(
                    HostUriBase,
                    callInformation.AICallType,
                    callInformation.PromptText,
                    DateTimeOffset.Now,
                    TimeSpan.Zero
                ), new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        var newRequest = BuildRequest(context, callInformation, mappedModelName);

        logger.LogDebug(
            "Rewritten URL from {OriginalUrl} to {NewUrl}. Incoming Model: {IncomingModelName}. Mapped Model: {MappedModelName}",
            context.Request.GetEncodedUrl(),
            newRequest!.RequestUri!.AbsoluteUri,
            callInformation.IncomingModelName,
            mappedModelName);

        var now = DateTimeOffset.Now;
        var sw = new Stopwatch();

        var typedDispatcher = context.RequestServices
            .GetRequiredService<ITypedHttpClientFactory<HttpAIEndpointDispatcher>>()
            .CreateClient(
                context.RequestServices.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(_id)
            );

        sw.Start();
        var openAiResponse = await typedDispatcher.Dispatch(newRequest, cancellationToken);

        //this will retry the operation for retryable status codes. When we reach here we might not want
        //to stream the response if it wasn't a 200.
        sw.Stop();

        //decision point... If this is a streaming request, then we should start streaming the result now.
        logger.LogDebug("Received Azure Open AI Response. Status Code: {StatusCode}", openAiResponse.StatusCode);

        var requestInformation =
            new AICentralRequestInformation(
                HostUriBase,
                callInformation.AICallType,
                callInformation.PromptText,
                now,
                sw.Elapsed);

        return (requestInformation, openAiResponse);
    }

    public virtual object WriteDebug()
    {
        return new
        {
            ModelMappings = _modelMappings,
        };
    }
    
    protected abstract string HostUriBase { get; }

    protected abstract HttpRequestMessage BuildRequest(
        HttpContext context,
        AICallInformation aiCallInformation,
        string mappedModelName);
}