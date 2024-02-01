using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using AICentral.Core;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using ActivitySource = AICentral.Core.ActivitySource;

namespace AICentral.Endpoints;

/// <summary>
/// Handles dispatching a call by using an IDownstreamEndpointAdapter
/// </summary>
internal class DownstreamEndpointDispatcher : IEndpointDispatcher
{
    private string EndpointName { get; }
    private readonly string _id;
    private readonly IDownstreamEndpointAdapter _iaiCentralDownstreamEndpointAdapter;
    private static readonly HttpResponseMessage RateLimitedFakeResponse = new(HttpStatusCode.TooManyRequests);

    public DownstreamEndpointDispatcher(IDownstreamEndpointAdapter iaiCentralDownstreamEndpointAdapter)
    {
        EndpointName = iaiCentralDownstreamEndpointAdapter.EndpointName;
        _id = iaiCentralDownstreamEndpointAdapter.Id;
        _iaiCentralDownstreamEndpointAdapter = iaiCentralDownstreamEndpointAdapter;
    }

    public async Task<AICentralResponse> Handle(
        HttpContext context,
        IncomingCallDetails callInformation,
        bool isLastChance,
        IResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<DownstreamEndpointDispatcher>>();
        var rateLimitingTracker = context.RequestServices.GetRequiredService<DownstreamEndpointRateLimitingTracker>();
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
                        null,
                        _iaiCentralDownstreamEndpointAdapter.BaseUrl.Host
                    ),
                    result!);
            }
            throw new HttpRequestException("Failed to satisfy request");
        }

        outboundRequest.Left(out var newRequest);

        if (rateLimitingTracker.IsRateLimiting(newRequest!.RequestUri!.Host, out var until))
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(until!.Value);
            RateLimitedFakeResponse.EnsureSuccessStatusCode();
        }

        logger.LogDebug(
            "Rewritten URL from {OriginalUrl} to {NewUrl}.",
            context.Request.GetEncodedUrl(),
            newRequest.RequestUri!.AbsoluteUri
        );

        using var source = ActivitySource.AICentralRequestActivitySource.CreateActivity(
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

        var openAiResponse = await typedDispatcher.Dispatch(newRequest, cancellationToken);

        //this will retry the operation for retryable status codes. When we reach here we might not want
        //to stream the response if it wasn't a 200.
        sw.Stop();

        if (openAiResponse.StatusCode == HttpStatusCode.TooManyRequests)
        {
            rateLimitingTracker.RateLimiting(newRequest.RequestUri.Host,
                openAiResponse.Headers.RetryAfter);
        }

        if (config.Value.EnableDiagnosticsHeaders)
        {
            if (openAiResponse.StatusCode == HttpStatusCode.OK)
            {
                context.Response.Headers.TryAdd("x-aicentral-server",
                    new StringValues(_iaiCentralDownstreamEndpointAdapter.BaseUrl.Host));
            }
            else
            {
                if (context.Response.Headers.TryGetValue("x-aicentral-failed-servers", out var header))
                {
                    context.Response.Headers.Remove("x-aicentral-failed-servers");
                }

                context.Response.Headers.TryAdd("x-aicentral-failed-servers",
                    StringValues.Concat(header, _iaiCentralDownstreamEndpointAdapter.BaseUrl.Host));
            }
        }

        //Blow up now if we didn't succeed and we _do_ have another option. We'll let the endpoint dispatcher catch this and deal with it (by choosing another endpoint) 
        if (!isLastChance)
        {
            openAiResponse.EnsureSuccessStatusCode();
        }

        var preProcessResult = await _iaiCentralDownstreamEndpointAdapter.ExtractResponseMetadata(
            callInformation,
            context,
            openAiResponse);


        var pipelineResponse = await responseGenerator.BuildResponse(
            new DownstreamRequestInformation(
                _iaiCentralDownstreamEndpointAdapter.BaseUrl.Host,
                callInformation.AICallType,
                callInformation.IncomingModelName,
                callInformation.PromptText,
                now,
                sw.Elapsed),
            context,
            openAiResponse,
            preProcessResult,
            cancellationToken);

        return pipelineResponse;
    }

    public bool IsAffinityRequestToMe(string affinityHeaderValue)
    {
        return EndpointName == affinityHeaderValue;
    }
}