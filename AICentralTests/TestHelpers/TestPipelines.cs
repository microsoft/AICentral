using AICentral;

namespace AICentralTests.TestHelpers;

public static class TestPipelines
{
    public static AICentralPipelineAssembler AzureOpenAIServiceWithRateLimitingAndSingleEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "random", "Model1")
            .WithRateLimiting(60, 1)
            .Assemble("azure-with-rate-limiter.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithSingleAzureOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "openai", "Model1")
            .Assemble("azure-openai-to-azure.localtest.me");

    public static AICentralPipelineAssembler OpenAIServiceWithSingleAzureOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "openai", "Model1")
            .Assemble("openai-to-azure.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithRandomAzureOpenAIEndpoints() =>
        new TestAICentralPipelineBuilder()
            .WithRandomEndpoints(new[]
            {
                (AICentralFakeResponses.Endpoint200, "random", "Model1"),
                (AICentralFakeResponses.Endpoint200Number2, "random", "Model1"),
            })
            .Assemble("azure-to-azure-openai.localtest.me");

    public static AICentralPipelineAssembler OpenAIServiceWithSingleOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleOpenAIEndpoint("openaiendpoint", "gpt-3.5-turbo")
            .Assemble("openai-to-openai.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithSingleOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleOpenAIEndpoint("openaiendpoint", "gpt-3.5-turbo")
            .Assemble("azure-openai-to-openai.localtest.me");

    public static AICentralPipelineAssembler AzureOpenAIServiceWithAuth() =>
        new TestAICentralPipelineBuilder()
            .WithApiKeyAuth("123", "456")
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
}