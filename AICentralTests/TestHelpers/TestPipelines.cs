using AICentral;

namespace AICentralTests.TestHelpers;

public static class TestPipelines
{
    public static AICentralPipelineAssembler ApiKeyAuth() =>
        new TestAICentralPipelineBuilder()
            .WithApiKeyAuth("api-key", "123", "456")
            .WithSingleEndpoint(AICentralTestEndpointBuilder.Endpoint200, "Model1", "Model1")
            .Assemble("/openai/deployments/api-key-auth/{*prefix}");

    public static AICentralPipelineAssembler RandomEndpointPickerNoAuth() =>
        new TestAICentralPipelineBuilder()
            .WithNoAuth()
            .WithRandomEndpoints(new[]
            {
                (AICentralTestEndpointBuilder.Endpoint200, "Model1", "Model1"),
                (AICentralTestEndpointBuilder.Endpoint200Number2, "Model1", "Model1"),
            })
            .Assemble("/openai/deployments/random/{*prefix}");

    public static AICentralPipelineAssembler PriorityEndpointPickerNoAuth() =>
        new TestAICentralPipelineBuilder()
            .WithNoAuth()
            .WithPriorityEndpoints(new[]
                {
                    (AICentralTestEndpointBuilder.Endpoint500, "Model1", "Model1"),
                    (AICentralTestEndpointBuilder.Endpoint404, "Model1", "Model1"),
                },
                new[]
                {
                    (AICentralTestEndpointBuilder.Endpoint200, "Model1", "Model1"),
                }
            )
            .Assemble("/openai/deployments/priority/{*prefix}");
}