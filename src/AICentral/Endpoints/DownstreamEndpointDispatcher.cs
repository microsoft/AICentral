using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AICentral.Core;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
namespace AICentral.Endpoints;

/// <summary>
/// Handles dispatching a call by using an IDownstreamEndpointAdapter
/// </summary>
internal class DownstreamEndpointDispatcher : IEndpointDispatcher
{
    private string EndpointName { get; }
    public string HostName => _iaiCentralDownstreamEndpointAdapter.BaseUrl.Host.ToLowerInvariant();
    private readonly string _id;
    private readonly IDownstreamEndpointAdapter _iaiCentralDownstreamEndpointAdapter;

    public DownstreamEndpointDispatcher(IDownstreamEndpointAdapter iaiCentralDownstreamEndpointAdapter)
    {
        EndpointName = iaiCentralDownstreamEndpointAdapter.EndpointName;
        _id = iaiCentralDownstreamEndpointAdapter.Id;
        _iaiCentralDownstreamEndpointAdapter = iaiCentralDownstreamEndpointAdapter;
    }

    public async Task<AICentralResponse> Handle(
        IRequestContext context,
        IncomingCallDetails callInformation,
        bool isLastChance,
        IResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<DownstreamEndpointDispatcher>>();
        var rateLimitingTracker = context.RequestServices.GetRequiredService<DownstreamEndpointResponseDataTracker>();
        var dateTimeProvider = context.RequestServices.GetRequiredService<IDateTimeProvider>();
        var config = context.RequestServices.GetRequiredService<IOptions<AICentralConfig>>();

        var outboundRequest = await _iaiCentralDownstreamEndpointAdapter.BuildRequest(callInformation, context);
        if (outboundRequest.Right(out var result))
        {
            if (isLastChance)
            {
                return new AICentralResponse(
                    DownstreamUsageInformation.Empty(
                        context,
                        callInformation,
                        _iaiCentralDownstreamEndpointAdapter.BaseUrl.Host,
                        EndpointName
                    ),
                    result!);
            }
            throw new HttpRequestException("Failed to satisfy request");
        }

        outboundRequest.Left(out var newRequest);

        var now = dateTimeProvider.Now;
        TimeSpan timeToResponse = TimeSpan.Zero;
        HttpResponseMessage? openAiResponse;
        bool addEndpointToFailed = true;

        if (rateLimitingTracker.IsRateLimiting(newRequest!.RequestUri!.Host, out var until))
        {
            openAiResponse = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            openAiResponse.Headers.RetryAfter = new RetryConditionHeaderValue(until!.Value);
            addEndpointToFailed = false;
        }
        else
        {
            logger.LogDebug(
                "Rewritten URL from {OriginalUrl} to {NewUrl}.",
                context.RequestEncodedUrl,
                newRequest.RequestUri!.AbsoluteUri
            );

            using var source = ActivitySource.AICentralRequestActivitySource.CreateActivity(
                "Calling AI Service",
                ActivityKind.Client,
                Activity.Current!.Context
            );
            var requestStopwatch = new Stopwatch();

            requestStopwatch.Start();

            openAiResponse = await _iaiCentralDownstreamEndpointAdapter.DispatchRequest(context, newRequest, cancellationToken);

            //this will retry the operation for retryable status codes. When we reach here we might not want
            //to stream the response if it wasn't a 200.
            requestStopwatch.Stop();
            timeToResponse = requestStopwatch.Elapsed;

            if (openAiResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitingTracker.RateLimiting(newRequest.RequestUri.Host,
                    openAiResponse.Headers.RetryAfter);
            }
        }

        var isBadRequest = openAiResponse.StatusCode == HttpStatusCode.BadRequest;
        if (config.Value.EnableDiagnosticsHeaders)
        {
            if (openAiResponse.StatusCode == HttpStatusCode.OK || isBadRequest)
            {
                context.ResponseHeaders.TryAdd("x-aicentral-server",
                    new StringValues(_iaiCentralDownstreamEndpointAdapter.BaseUrl.Host));
            }
            else if (addEndpointToFailed)
            {
                context.ResponseHeaders.Remove("x-aicentral-failed-servers", out var header);

                context.ResponseHeaders.TryAdd("x-aicentral-failed-servers",
                    StringValues.Concat(header, _iaiCentralDownstreamEndpointAdapter.BaseUrl.Host));
            }
        }

        //Blow up now if we didn't succeed and we _do_ have another option. We'll let the endpoint dispatcher catch this and deal with it (by choosing another endpoint) 
        //Make an exception for 400's. These come back for reasons like content safety triggers. I don't think we should retry these against other servers.
        if (!isLastChance && !isBadRequest)
        {
            openAiResponse.EnsureSuccessStatusCode();
        }

        var responseStopwatch = new Stopwatch();
        var preProcessResult = await _iaiCentralDownstreamEndpointAdapter.ExtractResponseMetadata(
            callInformation,
            context,
            openAiResponse);

        rateLimitingTracker.RecordMetrics(
            newRequest.RequestUri.Host, 
            preProcessResult.RemainingTokens,
            preProcessResult.RemainingRequests);

        var pipelineResponse = await responseGenerator.BuildResponse(
            new DownstreamRequestInformation(
                _iaiCentralDownstreamEndpointAdapter.BaseUrl.Host,
                EndpointName,
                callInformation.AICallType,
                callInformation.AICallResponseType,
                callInformation.IncomingModelName,
                callInformation.PromptText,
                now,
                timeToResponse),
            context,
            openAiResponse,
            preProcessResult,
            cancellationToken);

        responseStopwatch.Stop();
        var timeToRespondToConsumer = responseStopwatch.Elapsed;
        logger.LogDebug("AICentral took {DownstreamTime} to get initial response from AI Service and {ConsumerTime} to retransmit the response", timeToResponse, timeToRespondToConsumer);

        return pipelineResponse;
    }
    
    public bool IsAffinityRequestToMe(string affinityHeaderValue)
    {
        return EndpointName == affinityHeaderValue;
    }
    
    private class LastChanceRateLimitResult(string retryAfter) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = 429;
            httpContext.Response.Headers.RetryAfter = new StringValues(retryAfter);
            return Task.CompletedTask;  
        }
    }
}

