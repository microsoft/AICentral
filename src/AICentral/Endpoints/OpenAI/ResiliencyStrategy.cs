using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace AICentral.Endpoints.OpenAI;

public static class ResiliencyStrategy
{
    private static bool ShouldRetry(HttpResponseMessage response)
    {
        return (int)response.StatusCode >= 500 && (int)response.StatusCode < 599;
    }

    public static IAsyncPolicy<HttpResponseMessage> Build(int? maxConcurrency)
    {
        var handler = new PredicateBuilder<HttpResponseMessage>()
            .HandleResult(ShouldRetry);

        var policy = new ResiliencePipelineBuilder<HttpResponseMessage>()
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
            .AddTimeout(OpenAILikeDownstreamEndpointAdapter.MaxTimeToWaitForOpenAIResponse)
            .Build()
            .AsAsyncPolicy();

        if (maxConcurrency.HasValue)
        {
            return Policy.BulkheadAsync<HttpResponseMessage>(maxConcurrency.Value, 1000)
                .WrapAsync(policy);
        }

        return policy;
    }
}