using AICentral.Configuration;
using AICentral.RateLimiting;

namespace AICentralTests.TestHelpers;

public static class TestPipelines
{
     public static readonly string Endpoint500 = Guid.NewGuid().ToString();
     public static readonly string Endpoint404 = Guid.NewGuid().ToString();
     public static readonly string Endpoint200 = Guid.Parse("47bae1ca-d2f0-4584-b2ac-9897e7088919").ToString();
     public static readonly string Endpoint200Number2 = Guid.Parse("84bae1ca-d2f0-4584-b2ac-9897e708891a").ToString();
     public static readonly string FastEndpoint = Guid.NewGuid().ToString();
     public static readonly string SlowEndpoint = Guid.NewGuid().ToString();

     public static AICentralPipelineAssembler TokenPlusKeyEndpoint() =>
         new TestAICentralPipelineBuilder()
             .WithFakeEntraClientAuth()
             .WithSingleEndpointBearerPlusKey(Endpoint200)
             .WithBulkHead(5)
             .Assemble("bearer-plus-key.localtest.me");

     public static AICentralPipelineAssembler AzureOpenAILowestLatencyEndpoint() =>
         new TestAICentralPipelineBuilder()
             .WithLowestLatencyEndpoints(
                 (FastEndpoint, "random", "Model1"),
                 (SlowEndpoint, "random", "Model1")
             )
             .WithBulkHead(5)
             .Assemble("lowest-latency-tester.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithRateLimitingAndSingleEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200)
            .WithRateLimiting(60, 1)
            .Assemble("azure-with-rate-limiter.localtest.me");


    public static AICentralPipelineAssembler AzureOpenAIServiceWithClientPartitionedRateLimiter() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200)
            .WithRateLimiting(2, 1, RateLimitingLimitType.PerConsumer)
            .WithApiKeyAuth(
                ("client-1", "ignore-fake-key-123", "ignore-fake-key-234"),
                ("client-2", "ignore-fake-key-345", "ignore-fake-key-456")
            )
            .Assemble("azure-with-client-partitioned-rate-limiter.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithAutoUserPopulation() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200, autoPopulateUser: true)
            .WithApiKeyAuth(
                ("client-1", "ignore-fake-key-123", "ignore-fake-key-234"),
                ("client-2", "ignore-fake-key-345", "ignore-fake-key-456")
            )
            .Assemble("azure-with-auto-populate-user.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithTokenRateLimitingAndSingleEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200)
            .WithTokenRateLimiting(60, 50, RateLimitingLimitType.PerAICentralEndpoint)
            .Assemble("azure-with-token-rate-limiter.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithClientPartitionedTokenRateLimiter() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200)
            .WithTokenRateLimiting(2, 50, RateLimitingLimitType.PerConsumer)
            .WithApiKeyAuth(
                ("client-1", "ignore-fake-key-123", "ignore-fake-key-234"),
                ("client-2", "ignore-fake-key-345", "ignore-fake-key-456")
            )
            .Assemble("azure-with-client-partitioned-token-rate-limiter.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithSingleEndpointSelectorHierarchy() =>
        new TestAICentralPipelineBuilder()
            .WithHierarchicalEndpointSelector(Endpoint200, "random", "Model1")
            .Assemble("azure-hierarchical-selector.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithSingleAzureOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200)
            .Assemble("azure-openai-to-azure.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithChatImageFiltering() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200)
            .WithChatFiltering()
            .Assemble("azure-openai-to-azure-filter-chats.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithSingleAzureOpenAIEndpointWithMappedModel() =>
        new TestAICentralPipelineBuilder()
            .WithSingleMappedEndpoint(Endpoint200, "random", "mapped", true)
            .Assemble("azure-openai-to-azure-with-mapped-models.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithInBuiltJwtAuth()
    {
        return new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200)
            .WithCustomJwtProvider("jwt1", new Dictionary<string, string[]>()
            {
                ["azure-openai-to-azure-with_custom_jwt.localtest.me-pipeline"] = ["random"]
            })
            .Assemble("azure-openai-to-azure-with_custom_jwt.localtest.me");
    }

    public static AICentralPipelineAssembler AzureOpenAIServiceWithInBuiltWildcardJwtAuth()
    {
        return new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200)
            .WithCustomJwtProvider("jwt2", new Dictionary<string, string[]>()
            {
                ["azure-openai-to-azure-with_custom_jwt-wildcard.localtest.me-pipeline"] = ["*"],
            })
            .Assemble("azure-openai-to-azure-with_custom_jwt-wildcard.localtest.me");
    }

    public static AICentralPipelineAssembler AzureOpenAIServiceWith404Endpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint404)
            .Assemble("azure-openai-to-404.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithRandomAzureOpenAIEndpoints() =>
        new TestAICentralPipelineBuilder()
            .WithRandomEndpoints(
                (Endpoint200, "random", "Model1"),
                (Endpoint200Number2, "random", "Model1"))
            .Assemble("azure-to-azure-openai.localtest.me");


    public static AICentralPipelineAssembler AzureOpenAIServiceWithCapacityBasedAzureOpenAIEndpoints() =>
        new TestAICentralPipelineBuilder()
            .WithMetricsBasedEndpoints(
                (Endpoint200, "random", "Model1"),
                (Endpoint200Number2, "random", "Model1"))
            .Assemble("azure-to-azure-openai-capacity-based.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithSingleOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithRandomOpenAIEndpoints(new[]
                { ("openai-single", "ignore-fake-key-34523412", "openaimodel", "gpt-3.5-turbo") })
            .Assemble("azure-openai-to-openai.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithRandomOpenAIEndpoints() =>
        new TestAICentralPipelineBuilder()
            .WithRandomOpenAIEndpoints(
                new[]
                {
                    ("openai-rnd1", "ignore-fake-key-4323431", "openaimodel", "gpt-3.5-turbo"),
                    ("openai-rnd2", "ignore-fake-key-4323431", "openaimodel", "gpt-3.5-turbo")
                })
            .Assemble("azure-openai-to-multiple-openai.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithRandomOpenAIEndpointsDifferentModelMappings() =>
        new TestAICentralPipelineBuilder()
            .WithRandomOpenAIEndpoints(
                new[]
                {
                    ("openai-rnd1", "ignore-fake-key-67098981", "openaimodel1", "gpt-3.5-turbo"),
                    ("openai-rnd2", "ignore-fake-key-67098982", "openaimodel2", "gpt-3.5-turbo")
                })
            .Assemble("azure-openai-to-multiple-openai-different-model-mappings.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithAuth() =>
        new TestAICentralPipelineBuilder()
            .WithApiKeyAuth(("client-1", "ignore-fake-key-123", "ignore-fake-key-456"))
            .WithSingleEndpoint(Endpoint200)
            .Assemble("azure-with-auth.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithPriorityEndpointPickerNoAuth() =>
        new TestAICentralPipelineBuilder()
            .WithNoAuth()
            .WithPriorityEndpoints(new[]
                {
                    (Endpoint500, "priority", "Model1"),
                    (Endpoint404, "priority", "Model1"),
                },
                new[]
                {
                    (Endpoint200, "priority", "Model1"),
                }
            )
            .Assemble("azure-noauth-priority.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithBulkHeadOnPipelineAndSingleEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200)
            .WithBulkHead(5)
            .Assemble("azure-with-bulkhead.localtest.me");


    public static AICentralPipelineAssembler AzureOpenAIServiceWithBulkHeadOnSingleEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200, 5)
            .Assemble("azure-with-bulkhead-on-endpoint.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithAzureAISearchRouteProxy() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(Endpoint200)
            .WithAzureAISearchRouteProxy()
            .Assemble("azure-with-aisearch-route-proxy.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithRandomAffinityBasedAzureOpenAIEndpoints() =>
        new TestAICentralPipelineBuilder()
            .WithEndpointAffinity(TimeSpan.FromMinutes(1))
            .WithRandomEndpoints(
                (Endpoint200, "assistant-in", "ass-assistant-123-out"),
                (Endpoint200Number2, "assistant-in", "ass-assistant-123-out"))
            .WithApiKeyAuth(
                ("client-1", "ignore-fake-key-123", "ignore-fake-key-234"),
                ("client-2", "ignore-fake-key-345", "ignore-fake-key-456")
            )
            .Assemble("azure-to-azure-openai-random-with-affinity.localtest.me");
}