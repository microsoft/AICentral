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

public class TestAICentralPipelineBuilder
{
    private IAICentralClientAuthBuilder? _auth;
    private SingleEndpointSelectorBuilder? _endpointBuilder;
    private OpenAIEndpointDispatcherBuilder? _openAiEndpointDispatcherBuilder;

    public TestAICentralPipelineBuilder WithApiKeyAuth(string header, string key1, string key2)
    {
        _auth = new ApiKeyClientAuthBuilder(header, key1, key2);
        return this;
    }

    public TestAICentralPipelineBuilder WithSingleEndpoint(string hostname, string model, string mappedModel)
    {
        _openAiEndpointDispatcherBuilder = new OpenAIEndpointDispatcherBuilder($"https://{hostname}", new Dictionary<string, string>()
            {
                [model] = mappedModel
            },
            AuthenticationType.ApiKey,
            Guid.NewGuid().ToString());

        _endpointBuilder = new SingleEndpointSelectorBuilder(_openAiEndpointDispatcherBuilder);
        
        return this;
    }

    public AICentralPipelineAssembler Assemble(string path)
    {
        return  new AICentralPipelineAssembler(
            new Dictionary<string, Func<Dictionary<string, string>, IAICentralRouter>>()
            {
                ["PathMatch"] = SimplePathMatchRouter.BuildFromConfig
            },
            new Dictionary<string, IAICentralClientAuthBuilder>()
            {
                ["test"] = _auth!,
            },
            new Dictionary<string, IAICentralEndpointDispatcherBuilder>()
            {
                ["test"] = _openAiEndpointDispatcherBuilder!
            },
            new Dictionary<string, IAICentralEndpointSelectorBuilder>()
            {
                ["test"] = _endpointBuilder!
            },
            new Dictionary<string, IAICentralPipelineStepBuilder<IAICentralPipelineStep>>(),
            new[]
            {
                new ConfigurationTypes.AICentralPipelineConfig()
                {
                    Name = Guid.NewGuid().ToString(),
                    Path = new ConfigurationTypes.AICentralComponentConfig()
                    {
                        Type = "PathMatch",
                        Properties = new Dictionary<string, string>
                        {
                            ["Path"] = path,
                        }
                    },
                    AuthProvider = "test",
                    Steps = Array.Empty<string>(),
                    EndpointSelector = "test"
                }
            }
        );
        
    }

}