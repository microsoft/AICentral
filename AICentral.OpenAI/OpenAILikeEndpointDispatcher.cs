using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using AICentral.Core;
using AICentral.OpenAI.OpenAI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AICentral.OpenAI;

public abstract class OpenAILikeEndpointDispatcher : IAICentralEndpointDispatcher
{
    protected string EndpointName { get; }
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _id;
    private static readonly HttpResponseMessage RateLimitedFakeResponse = new(HttpStatusCode.TooManyRequests);

    private static readonly HashSet<string> HeadersToIgnore = new(new[] { "host", "authorization", "api-key" });

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
        IAICentralResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<OpenAIEndpointDispatcherFactory>>();
        var rateLimitingTracker = context.RequestServices.GetRequiredService<InMemoryRateLimitingTracker>();
        var dateTimeProvider = context.RequestServices.GetRequiredService<IDateTimeProvider>();
        var config = context.RequestServices.GetRequiredService<IOptions<AICentralConfig>>();

        var incomingModelName = callInformation.IncomingCallDetails.IncomingModelName ?? string.Empty;

        var mappedModelName = _modelMappings.GetValueOrDefault(incomingModelName, incomingModelName);

        if (MappedModelFoundAsEmptyString(callInformation, mappedModelName))
        {
            return new AICentralResponse(AICentralUsageInformation.Empty(context, callInformation.IncomingCallDetails, HostUriBase), Results.NotFound(new { message = "Unknown model" }));
        }

        HttpRequestMessage? newRequest = default;
        try
        {
            newRequest = BuildNewRequest(context, callInformation, mappedModelName);
        }
        catch (InvalidOperationException ie)
        {
            return new AICentralResponse(AICentralUsageInformation.Empty(context, callInformation.IncomingCallDetails, HostUriBase), Results.BadRequest(new { message = ie.Message }));
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

        if (config.Value.EnableDiagnosticsHeaders)
        {
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
        }

        //Blow up if we didn't succeed and we don't have another option.
        if (!isLastChance)
        {
            openAiResponse.EnsureSuccessStatusCode();
        }

        await ExtractDiagnostics(callInformation.IncomingCallDetails, mappedModelName, newRequest!, openAiResponse);
        
        return await responseGenerator.BuildResponse(
            new DownstreamRequestInformation(
                HostUriBase,
                callInformation.IncomingCallDetails.AICallType,
                callInformation.IncomingCallDetails.PromptText,
                now,
                sw.Elapsed),
            context, 
            openAiResponse, 
            SanitiseHeaders(context, openAiResponse), cancellationToken);
    }

    /// <summary>
    /// Opportunity to pull specific diagnostics and, for example, raise your own telemetry events.
    /// </summary>
    /// <param name="incomingCallDetails"></param>
    /// <param name="mappedModelName"></param>
    /// <param name="downstreamRequest"></param>
    /// <param name="openAiResponse"></param>
    /// <returns></returns>
    protected abstract Task ExtractDiagnostics(IncomingCallDetails incomingCallDetails, string mappedModelName,
        HttpRequestMessage downstreamRequest,
        HttpResponseMessage openAiResponse);

    public bool IsAffinityRequestToMe(string affinityHeaderValue)
    {
        return EndpointName == affinityHeaderValue;
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