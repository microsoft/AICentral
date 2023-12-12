using AICentral;
using AICentral.Configuration;
using AICentral.RateLimiting;

namespace AICentralTests.TestHelpers;

public static class TestPipelines
{
    public static AICentralPipelineAssembler AzureOpenAILowestLatencyEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithLowestLatencyEndpoints(
                (AICentralFakeResponses.FastEndpoint, "random", "Model1"),
                (AICentralFakeResponses.SlowEndpoint, "random", "Model1")
            )
            .WithBulkHead(5)
            .Assemble("lowest-latency-tester.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithRateLimitingAndSingleEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "random", "Model1")
            .WithRateLimiting(60, 1)
            .Assemble("azure-with-rate-limiter.localtest.me");

    
    public static AICentralPipelineAssembler AzureOpenAIServiceWithClientPartitionedRateLimiter() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "random", "Model1")
            .WithRateLimiting(2, 1, RateLimitingLimitType.PerConsumer)
            .WithApiKeyAuth(
                ("client-1", "123", "234"),
                ("client-2", "345", "456")
            )
            .Assemble("azure-with-client-partitioned-rate-limiter.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithTokenRateLimitingAndSingleEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "random", "Model1")
            .WithTokenRateLimiting(60, 50, RateLimitingLimitType.PerAICentralEndpoint)
            .Assemble("azure-with-token-rate-limiter.localtest.me");
    
    public static AICentralPipelineAssembler AzureOpenAIServiceWithClientPartitionedTokenRateLimiter() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "random", "Model1")
            .WithTokenRateLimiting(2, 50, RateLimitingLimitType.PerConsumer)
            .WithApiKeyAuth(
                ("client-1", "123", "234"),
                ("client-2", "345", "456")
            )
            .Assemble("azure-with-client-partitioned-token-rate-limiter.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithSingleEndpointSelectorHierarchy() =>
        new TestAICentralPipelineBuilder()
            .WithHierarchicalEndpointSelector(AICentralFakeResponses.Endpoint200, "random", "Model1")
            .Assemble("azure-hierarchical-selector.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithSingleAzureOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "gpt-3.5-turbo", "Model1")
            .Assemble("azure-openai-to-azure.localtest.me");

    public static AICentralPipelineAssembler OpenAIServiceWithSingleAzureOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "gpt-3.5-turbo", "Model1")
            .Assemble("openai-to-azure.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithRandomAzureOpenAIEndpoints() =>
        new TestAICentralPipelineBuilder()
            .WithRandomEndpoints(
                (AICentralFakeResponses.Endpoint200, "random", "Model1"),
                (AICentralFakeResponses.Endpoint200Number2, "random", "Model1"))
            .Assemble("azure-to-azure-openai.localtest.me");

    public static AICentralPipelineAssembler OpenAIServiceWithSingleOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleOpenAIEndpoint("openai-single", "openaimodel", "gpt-3.5-turbo")
            .Assemble("openai-to-openai.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithSingleOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleOpenAIEndpoint("openai-single", "openaimodel", "gpt-3.5-turbo")
            .Assemble("azure-openai-to-openai.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithAuth() =>
        new TestAICentralPipelineBuilder()
            .WithApiKeyAuth(("client-1", "123", "456"))
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "api-key-auth", "Model1")
            .Assemble("azure-with-auth.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithPriorityEndpointPickerNoAuth() =>
        new TestAICentralPipelineBuilder()
            .WithNoAuth()
            .WithPriorityEndpoints(new[]
                {
                    (AICentralFakeResponses.Endpoint500, "priority", "Model1"),
                    (AICentralFakeResponses.Endpoint404, "priority", "Model1"),
                },
                new[]
                {
                    (AICentralFakeResponses.Endpoint200, "priority", "Model1"),
                }
            )
            .Assemble("azure-noauth-priority.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithBulkHeadOnPipelineAndSingleEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "Model1", "Model1", null)
            .WithBulkHead(5)
            .Assemble("azure-with-bulkhead.localtest.me");


    public static AICentralPipelineAssembler AzureOpenAIServiceWithBulkHeadOnSingleEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "Model1", "Model1", 5)
            .Assemble("azure-with-bulkhead-on-endpoint.localtest.me");
}