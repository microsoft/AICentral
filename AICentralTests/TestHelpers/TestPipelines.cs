using AICentral;
using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Auth;
using AICentral.PipelineComponents.Auth.ApiKey;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.OpenAI;
using AICentral.PipelineComponents.EndpointSelectors;
using AICentral.PipelineComponents.EndpointSelectors.Single;
using AICentral.PipelineComponents.Routes;

namespace AICentralTests.TestHelpers;

public static class TestPipelines
{
    private static OpenAIEndpointDispatcherBuilder Builder200 = new(
        $"http://{AICentralTestEndpointBuilder.Endpoint200}",
        new Dictionary<string, string>()
        {
            ["Model1"] = "Model1"
        },
        AuthenticationType.ApiKey, "999");

    public static AICentralPipelineAssembler ApiKeyAuth() => new AICentralPipelineAssembler(
        new Dictionary<string, Func<Dictionary<string, string>, IAICentralRouter>>()
        {
            ["PathMatch"] = SimplePathMatchRouter.BuildFromConfig
        },
        new Dictionary<string, IAICentralClientAuthBuilder>()
        {
            ["apikey"] = new ApiKeyClientAuthBuilder("api-key", "123", "456"),
        },
        new Dictionary<string, IAICentralEndpointDispatcherBuilder>()
        {
            ["simple"] = Builder200
        },
        new Dictionary<string, IAICentralEndpointSelectorBuilder>()
        {
            ["simple"] = new SingleEndpointSelectorBuilder(Builder200)
        },
        new Dictionary<string, IAICentralPipelineStepBuilder<IAICentralPipelineStep>>(),
        new[]
        {
            new ConfigurationTypes.AICentralPipelineConfig()
            {
                Name = "ApiKey",
                Path = new ConfigurationTypes.AICentralComponentConfig()
                {
                    Type = "PathMatch",
                    Properties = new Dictionary<string, string>
                    {
                        ["Path"] ="/openai/deployments/api-key-auth/{*prefix}",
                    }
                },
                AuthProvider = "apikey",
                Steps = Array.Empty<string>(),
                EndpointSelector = "simple"
            }
        }
    );
}