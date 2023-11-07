using AICentral;
using AICentral.Configuration.JSON;

namespace AICentralTests.TestHelpers;

public static class TestPipelines
{
    public static AICentralPipelineAssembler OpenAIService() =>
        new TestAICentralPipelineBuilder()
            .WithEndpointType(EndpointType.OpenAI)
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "openai", "Model1")
            .Assemble("/v1/{*prefix}");

    public static AICentralPipelineAssembler OpenAICatchAllEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "openai", "Model1")
            .Assemble("/openai/{*rest}");

    public static AICentralPipelineAssembler WithOpenAIEndpoint() =>
        new TestAICentralPipelineBuilder()
            .WithSingleOpenAIEndpoint("openaiendpoint", "gpt-3.5-turbo")
            .Assemble("/openai/deployments/openaiendpoint/{*prefix}");

    public static AICentralPipelineAssembler ApiKeyAuth() =>
        new TestAICentralPipelineBuilder()
            .WithApiKeyAuth("123", "456")
            .WithSingleEndpoint(AICentralFakeResponses.Endpoint200, "api-key-auth", "Model1")
            .Assemble("/openai/deployments/api-key-auth/{*prefix}");

    public static AICentralPipelineAssembler RandomEndpointPickerNoAuth() =>
        new TestAICentralPipelineBuilder()
            .WithNoAuth()
            .WithRandomEndpoints(new[]
            {
                (AICentralFakeResponses.Endpoint200, "random", "Model1"),
                (AICentralFakeResponses.Endpoint200Number2, "random", "Model1"),
            })
            .Assemble("/openai/deployments/random/{*prefix}");

    public static AICentralPipelineAssembler PriorityEndpointPickerNoAuth() =>
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
            .Assemble("/openai/deployments/priority/{*prefix}");
}