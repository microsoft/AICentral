using System.Net;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace AICentral.PipelineComponents.Endpoints;

public static class ResiliencyStrategy
{
    private static readonly HttpStatusCode[] StatusCodesToRetry = { HttpStatusCode.TooManyRequests };

    public static IAsyncPolicy<HttpResponseMessage> Build()
    {
        var handler = new PredicateBuilder<HttpResponseMessage>()
            .HandleResult(r => StatusCodesToRetry.Contains(r.StatusCode))
            .Handle<HttpRequestException>(e =>
                e.StatusCode.HasValue && StatusCodesToRetry.Contains(e.StatusCode.Value));

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = handler
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                Delay = TimeSpan.FromSeconds(0.2),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = 3,
                ShouldHandle = handler
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build()
            .AsAsyncPolicy();
    }
}