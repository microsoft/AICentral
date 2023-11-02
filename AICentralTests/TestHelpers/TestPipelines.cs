using AICentral;

namespace AICentralTests.TestHelpers;

public static class TestPipelines
{
    public static AICentralPipelineAssembler ApiKeyAuth() =>
        new TestAICentralPipelineBuilder()
            .WithApiKeyAuth("api-key", "123", "456")
            .WithSingleEndpoint(AICentralTestEndpointBuilder.Endpoint200, "api-key-auth", "Model1")
            .Assemble("/openai/deployments/api-key-auth/{*prefix}");

    public static AICentralPipelineAssembler RandomEndpointPickerNoAuth() =>
        new TestAICentralPipelineBuilder()
            .WithNoAuth()
            .WithRandomEndpoints(new[]
            {
                (AICentralTestEndpointBuilder.Endpoint200, "random", "Model1"),
                (AICentralTestEndpointBuilder.Endpoint200Number2, "random", "Model1"),
            })
            .Assemble("/openai/deployments/random/{*prefix}");

    public static AICentralPipelineAssembler PriorityEndpointPickerNoAuth() =>
        new TestAICentralPipelineBuilder()
            .WithNoAuth()
            .WithPriorityEndpoints(new[]
                {
                    (AICentralTestEndpointBuilder.Endpoint500, "priority", "Model1"),
                    (AICentralTestEndpointBuilder.Endpoint404, "priority", "Model1"),
                },
                new[]
                {
                    (AICentralTestEndpointBuilder.Endpoint200, "priority", "Model1"),
                }
            )
            .Assemble("/openai/deployments/priority/{*prefix}");
}