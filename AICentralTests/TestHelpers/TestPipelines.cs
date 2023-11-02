using AICentral;

namespace AICentralTests.TestHelpers;

public static class TestPipelines
{
    public static AICentralPipelineAssembler ApiKeyAuth() =>
        new TestAICentralPipelineBuilder()
            .WithApiKeyAuth("api-key", "123", "456")
            .WithSingleEndpoint(AICentralTestEndpointBuilder.Endpoint200, "Model1", "Model1")
            .Assemble("/openai/deployments/api-key-auth/{*prefix}");
    
}