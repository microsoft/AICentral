using System.Diagnostics;
using System.Net;
using AICentral.Steps.Endpoints.OpenAILike.OpenAI;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.Endpoints.OpenAILike;

public abstract class OpenAILikeEndpointDispatcher : IAICentralEndpointDispatcher
{
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _id;
    private static readonly string[] HeadersToIgnore = new[] { "Host" }; 

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
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<OpenAIEndpointDispatcherBuilder>>();

        var incomingModelName = callInformation.IncomingCallDetails.IncomingModelName ?? string.Empty;
        
        var mappedModelName = _modelMappings.TryGetValue(incomingModelName, out var mapping)
            ? mapping
            : incomingModelName;

        if (MappedModelFoundAsEmptyString(callInformation, mappedModelName))
        {
            return (
                new AICentralRequestInformation(
                    HostUriBase,
                    callInformation.IncomingCallDetails.AICallType,
                    callInformation.IncomingCallDetails.PromptText,
                    DateTimeOffset.Now,
                    TimeSpan.Zero
                ), new HttpResponseMessage(HttpStatusCode.NotFound));
        }


        try
        {
            var newRequest = await BuildNewRequest(context, callInformation, mappedModelName);
            await CustomiseRequest(context, callInformation, newRequest, mappedModelName);

            logger.LogDebug(
                "Rewritten URL from {OriginalUrl} to {NewUrl}. Incoming Model: {IncomingModelName}. Mapped Model: {MappedModelName}",
                context.Request.GetEncodedUrl(),
                newRequest.RequestUri!.AbsoluteUri,
                incomingModelName,
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
                    callInformation.IncomingCallDetails.AICallType,
                    callInformation.IncomingCallDetails.PromptText,
                    now,
                    sw.Elapsed);

            return (requestInformation, openAiResponse);
        }
        catch (NotSupportedException ne)
        {
            logger.LogWarning(ne, "Invalid usage detected");
            return (new AICentralRequestInformation(
                    HostUriBase,
                    AICallType.Other,
                    callInformation.IncomingCallDetails.PromptText,
                    DateTimeOffset.Now,
                    TimeSpan.Zero),
                new HttpResponseMessage(HttpStatusCode.BadRequest));
        }
    }

    private static bool MappedModelFoundAsEmptyString(AICallInformation callInformation, string mappedModelName)
    {
        return callInformation.IncomingCallDetails.AICallType != AICallType.Other && mappedModelName == string.Empty;
    }

    private Task<HttpRequestMessage> BuildNewRequest(HttpContext context, AICallInformation callInformation, string? mappedModelName)
    {
        var newRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), BuildUri(context, callInformation, mappedModelName));
        foreach (var header in context.Request.Headers)
        {
            if (HeadersToIgnore.Contains(header.Key)) continue;
            
            if (!newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) &&
                newRequest.Content != null)
            {
                newRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
        return Task.FromResult(newRequest);
    }

    protected abstract Task CustomiseRequest(HttpContext context, AICallInformation callInformation, HttpRequestMessage newRequest, string? newModelName);

    public virtual object WriteDebug()
    {
        return new
        {
            ModelMappings = _modelMappings,
        };
    }

    public abstract Dictionary<string, StringValues> SanitiseHeaders(HttpContext context, HttpResponseMessage openAiResponse);

    protected abstract string HostUriBase { get; }

    protected abstract string BuildUri(HttpContext context, AICallInformation aiCallInformation, string? mappedModelName);
}