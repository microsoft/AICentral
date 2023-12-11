using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using AICentral.Core;
using AICentral.Steps.Endpoints.OpenAILike.OpenAI;
using AICentral.Steps.EndpointSelectors;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.DeepDev;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.Endpoints.OpenAILike;

public abstract class OpenAILikeEndpointDispatcher : IAICentralEndpointDispatcher
{
    public string EndpointName { get; }
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _id;
    private static readonly HttpResponseMessage RateLimitedFakeResponse = new(HttpStatusCode.TooManyRequests);

    private static readonly HashSet<string> HeadersToIgnore = new(new[] { "host", "authorization", "api-key" });

    private static readonly Dictionary<string, ITokenizer> Tokenisers = new()
    {
        ["gpt-3.5-turbo-0613"] = TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo").Result,
        ["gpt-35-turbo"] = TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo").Result,
        ["gpt-4"] = TokenizerBuilder.CreateByModelNameAsync("gpt-4").Result,
    };

    protected OpenAILikeEndpointDispatcher(
        string id,
        string endpointName,
        Dictionary<string, string> modelMappings)
    {
        EndpointName = endpointName;
        _id = id;
        _modelMappings = modelMappings;
    }

    public async Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation callInformation,
        bool isLastChance,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<OpenAIEndpointDispatcherFactory>>();
        var rateLimitingTracker = context.RequestServices.GetRequiredService<InMemoryRateLimitingTracker>();
        var dateTimeProvider = context.RequestServices.GetRequiredService<IDateTimeProvider>();

        var incomingModelName = callInformation.IncomingCallDetails.IncomingModelName ?? string.Empty;

        var mappedModelName = _modelMappings.GetValueOrDefault(incomingModelName, incomingModelName);

        if (MappedModelFoundAsEmptyString(callInformation, mappedModelName))
        {
            return new AICentralResponse(
                new AICentralUsageInformation(
                    HostUriBase,
                    string.Empty,
                    context.User.Identity?.Name ?? "unknown",
                    callInformation.IncomingCallDetails.AICallType,
                    callInformation.IncomingCallDetails.PromptText,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    dateTimeProvider.Now, TimeSpan.Zero
                ), Results.NotFound(new { message = "Unknown model" }));
        }

        HttpRequestMessage? newRequest = default;
        try
        {
            newRequest = BuildNewRequest(context, callInformation, mappedModelName);
        }
        catch (InvalidOperationException ie)
        {
            return new AICentralResponse(
                new AICentralUsageInformation(
                    HostUriBase,
                    string.Empty,
                    context.User.Identity?.Name ?? "unknown",
                    callInformation.IncomingCallDetails.AICallType,
                    callInformation.IncomingCallDetails.PromptText,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    dateTimeProvider.Now, TimeSpan.Zero
                ), Results.BadRequest(new { message = ie.Message }));
        }

        if (rateLimitingTracker.IsRateLimiting(newRequest.RequestUri!.Host, out var until))
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(until!.Value);
            RateLimitedFakeResponse.EnsureSuccessStatusCode();
        }


        await CustomiseRequest(context, callInformation, newRequest!, mappedModelName);

        logger.LogDebug(
            "Rewritten URL from {OriginalUrl} to {NewUrl}. Incoming Model: {IncomingModelName}. Mapped Model: {MappedModelName}",
            context.Request.GetEncodedUrl(),
            newRequest.RequestUri!.AbsoluteUri,
            incomingModelName,
            mappedModelName);

        using var source = AICentralActivitySource.AICentralRequestActivitySource.CreateActivity(
            "Calling AI Service",
            ActivityKind.Client,
            Activity.Current!.Context,
            new Dictionary<string, object?>()
            {
                ["ModelName"] = mappedModelName,
                ["ServiceType"] = callInformation.IncomingCallDetails.ServiceType
            }
        );
        var now = dateTimeProvider.Now;
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

        if (openAiResponse.StatusCode == HttpStatusCode.TooManyRequests)
        {
            rateLimitingTracker.RateLimiting(newRequest.RequestUri.Host, openAiResponse.Headers.RetryAfter);
        }

        if (openAiResponse.StatusCode == HttpStatusCode.OK)
        {
            context.Response.Headers.TryAdd("x-aicentral-server", new StringValues(HostUriBase));
        }
        else
        {
            if (context.Response.Headers.TryGetValue("x-aicentral-failed-servers", out var header))
            {
                context.Response.Headers.Remove("x-aicentral-failed-servers");
            }

            context.Response.Headers.TryAdd("x-aicentral-failed-servers", StringValues.Concat(header, HostUriBase));
        }

        //Blow up if we didn't succeed and we don't have another option.
        if (!isLastChance)
        {
            openAiResponse.EnsureSuccessStatusCode();
        }

        //decision point... If this is a streaming request, then we should start streaming the result now.
        logger.LogDebug("Received Azure Open AI Response. Status Code: {StatusCode}", openAiResponse.StatusCode);

        var requestInformation =
            new AICentralRequestInformation(
                HostUriBase,
                callInformation.IncomingCallDetails.AICallType,
                callInformation.IncomingCallDetails.PromptText,
                now,
                sw.Elapsed);

        CopyHeadersToResponse(context.Response, SanitiseHeaders(context, openAiResponse));

        if (openAiResponse.Headers.TransferEncodingChunked == true)
        {
            logger.LogDebug("Detected chunked encoding response. Streaming response back to consumer");
            return await ServerSideEventResponseHandler.Handle(
                Tokenisers,
                context,
                cancellationToken,
                openAiResponse,
                requestInformation);
        }

        if ((openAiResponse.Content.Headers.ContentType?.MediaType ?? string.Empty).Contains(
                "json",
                StringComparison.InvariantCultureIgnoreCase))
        {
            logger.LogDebug("Detected non-chunked encoding response. Sending response back to consumer");
            return await JsonResponseHandler.Handle(
                context,
                cancellationToken,
                openAiResponse,
                requestInformation);
        }

        return await StreamResponseHandler.Handle(
            context,
            cancellationToken,
            openAiResponse,
            requestInformation);
    }

    public bool IsAffinityRequestToMe(string affinityHeaderValue)
    {
        return EndpointName == affinityHeaderValue;
    }


    private static void CopyHeadersToResponse(HttpResponse response, Dictionary<string, StringValues> headersToProxy)
    {
        foreach (var header in headersToProxy)
        {
            response.Headers.TryAdd(header.Key, header.Value);
        }
    }

    private static bool MappedModelFoundAsEmptyString(AICallInformation callInformation, string mappedModelName)
    {
        return callInformation.IncomingCallDetails.AICallType != AICallType.Other && mappedModelName == string.Empty;
    }

    private HttpRequestMessage BuildNewRequest(HttpContext context, AICallInformation callInformation,
        string? mappedModelName)
    {
        var newRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method),
            BuildUri(context, callInformation, mappedModelName));
        foreach (var header in context.Request.Headers)
        {
            if (HeadersToIgnore.Contains(header.Key.ToLowerInvariant())) continue;

            if (!newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) &&
                newRequest.Content != null)
            {
                newRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        return newRequest;
    }

    protected abstract Task CustomiseRequest(HttpContext context, AICallInformation callInformation,
        HttpRequestMessage newRequest, string? newModelName);

    protected abstract Dictionary<string, StringValues> SanitiseHeaders(HttpContext context,
        HttpResponseMessage openAiResponse);

    protected abstract string HostUriBase { get; }

    protected abstract string BuildUri(HttpContext context, AICallInformation aiCallInformation,
        string? mappedModelName);
}