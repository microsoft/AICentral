using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using AICentral.Core;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AICentral.Endpoints;

public class DownstreamEndpointDispatcher : IAICentralEndpointDispatcher
{
    private string EndpointName { get; }
    private readonly string _id;
    private readonly IEndpointAdapter _endpointDispatcher;
    private static readonly HttpResponseMessage RateLimitedFakeResponse = new(HttpStatusCode.TooManyRequests);

    public DownstreamEndpointDispatcher(IEndpointAdapter endpointDispatcher)
    {
        EndpointName = endpointDispatcher.EndpointName;
        _id = endpointDispatcher.Id;
        _endpointDispatcher = endpointDispatcher;
    }

    public async Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation callInformation,
        bool isLastChance,
        IAICentralResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<DownstreamEndpointDispatcher>>();
        var rateLimitingTracker = context.RequestServices.GetRequiredService<DownstreamEndpointRateLimitingTracker>();
        var dateTimeProvider = context.RequestServices.GetRequiredService<IDateTimeProvider>();
        var config = context.RequestServices.GetRequiredService<IOptions<AICentralConfig>>();

        var outboundRequest = await _endpointDispatcher.BuildRequest(callInformation, context);
        if (outboundRequest.Right(out var result))
            return new AICentralResponse(
                DownstreamUsageInformation.Empty(context, callInformation.IncomingCallDetails,
                    _endpointDispatcher.BaseUrl), result!);

        outboundRequest.Left(out var newRequest);

        if (rateLimitingTracker.IsRateLimiting(newRequest!.HttpRequestMessage.RequestUri!.Host, out var until))
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(until!.Value);
            RateLimitedFakeResponse.EnsureSuccessStatusCode();
        }

        logger.LogDebug(
            "Rewritten URL from {OriginalUrl} to {NewUrl}.",
            context.Request.GetEncodedUrl(),
            newRequest.HttpRequestMessage.RequestUri!.AbsoluteUri
        );

        using var source = AICentralActivitySource.AICentralRequestActivitySource.CreateActivity(
            "Calling AI Service",
            ActivityKind.Client,
            Activity.Current!.Context
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

        var openAiResponse = await typedDispatcher.Dispatch(context.Request, newRequest.HttpRequestMessage, cancellationToken);

        //this will retry the operation for retryable status codes. When we reach here we might not want
        //to stream the response if it wasn't a 200.
        sw.Stop();

        if (openAiResponse.StatusCode == HttpStatusCode.TooManyRequests)
        {
            rateLimitingTracker.RateLimiting(newRequest.HttpRequestMessage.RequestUri.Host,
                openAiResponse.Headers.RetryAfter);
        }

        if (config.Value.EnableDiagnosticsHeaders)
        {
            if (openAiResponse.StatusCode == HttpStatusCode.OK)
            {
                context.Response.Headers.TryAdd("x-aicentral-server", new StringValues(_endpointDispatcher.BaseUrl));
            }
            else
            {
                if (context.Response.Headers.TryGetValue("x-aicentral-failed-servers", out var header))
                {
                    context.Response.Headers.Remove("x-aicentral-failed-servers");
                }

                context.Response.Headers.TryAdd("x-aicentral-failed-servers",
                    StringValues.Concat(header, _endpointDispatcher.BaseUrl));
            }
        }

        //Blow up if we didn't succeed and we don't have another option.
        if (!isLastChance)
        {
            openAiResponse.EnsureSuccessStatusCode();
        }

        var preProcessResult = await _endpointDispatcher.ExtractResponseMetadata(
            callInformation.IncomingCallDetails,
            context,
            newRequest,
            openAiResponse);

        EmitTelemetry(newRequest, preProcessResult);

        return await responseGenerator.BuildResponse(
            new DownstreamRequestInformation(
                _endpointDispatcher.BaseUrl,
                callInformation.IncomingCallDetails.AICallType,
                callInformation.IncomingCallDetails.PromptText,
                now,
                sw.Elapsed),
            context,
            openAiResponse,
            preProcessResult.SanitisedHeaders,
            cancellationToken);
    }

    private void EmitTelemetry(AIRequest request, ResponseMetadata responseMetadata)
    {
        if (responseMetadata.RemainingRequests != null)
        {
            AICentralActivitySources.RecordGaugeMetric(
                "remaining-requests",
                request.HttpRequestMessage.RequestUri!.Host, 
                request.ModelName ?? "<no-model>",
                responseMetadata.RemainingRequests.Value);
        }

        if (responseMetadata.RemainingTokens != null)
        {
            AICentralActivitySources.RecordGaugeMetric(
                "remaining-tokens", 
                request.HttpRequestMessage.RequestUri!.Host,
                request.ModelName ?? "<no-model>", 
                responseMetadata.RemainingTokens.Value);
        }
    }

    public bool IsAffinityRequestToMe(string affinityHeaderValue)
    {
        return EndpointName == affinityHeaderValue;
    }
}