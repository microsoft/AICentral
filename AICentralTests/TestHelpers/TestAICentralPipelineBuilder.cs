using AICentral;
using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Auth;
using AICentral.PipelineComponents.Auth.AllowAnonymous;
using AICentral.PipelineComponents.Auth.ApiKey;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.OpenAI;
using AICentral.PipelineComponents.EndpointSelectors;
using AICentral.PipelineComponents.EndpointSelectors.Priority;
using AICentral.PipelineComponents.EndpointSelectors.Random;
using AICentral.PipelineComponents.EndpointSelectors.Single;
using AICentral.PipelineComponents.Routes;

namespace AICentralTests.TestHelpers;

public class TestAICentralPipelineBuilder
{
    private IAICentralClientAuthBuilder? _auth;
    private IAICentralEndpointSelectorBuilder? _endpointBuilder;
    private IAICentralEndpointDispatcherBuilder[]? _openAiEndpointDispatcherBuilders;

    public TestAICentralPipelineBuilder WithApiKeyAuth(string header, string key1, string key2)
    {
        _auth = new ApiKeyClientAuthBuilder(header, key1, key2);
        return this;
    }

    public TestAICentralPipelineBuilder WithNoAuth()
    {
        _auth = new AllowAnonymousClientAuthBuilder();
        return this;
    }

    public TestAICentralPipelineBuilder WithSingleEndpoint(string hostname, string model, string mappedModel)
    {
        var openAiEndpointDispatcherBuilder = new OpenAIEndpointDispatcherBuilder($"https://{hostname}",
            new Dictionary<string, string>()
            {
                [model] = mappedModel
            },
            AuthenticationType.ApiKey,
            Guid.NewGuid().ToString());

        _endpointBuilder = new SingleEndpointSelectorBuilder(openAiEndpointDispatcherBuilder);
        _openAiEndpointDispatcherBuilders = new[] { openAiEndpointDispatcherBuilder };

        return this;
    }


    public TestAICentralPipelineBuilder WithPriorityEndpoints(
        (string hostname, string model, string mappedModel)[] priorityEndpoints,
        (string hostname, string model, string mappedModel)[] fallbackEndpoints
    )
    {
        IAICentralEndpointDispatcherBuilder[] priorityOpenAiEndpointDispatcherBuilder = priorityEndpoints.Select(x =>
            new OpenAIEndpointDispatcherBuilder($"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                AuthenticationType.ApiKey,
                Guid.NewGuid().ToString())).ToArray();

        IAICentralEndpointDispatcherBuilder[] fallbackOpenAiEndpointDispatcherBuilder = fallbackEndpoints.Select(x =>
            new OpenAIEndpointDispatcherBuilder($"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                AuthenticationType.ApiKey,
                Guid.NewGuid().ToString())).ToArray();

        _openAiEndpointDispatcherBuilders = priorityOpenAiEndpointDispatcherBuilder
            .Union(fallbackOpenAiEndpointDispatcherBuilder).ToArray();
        
        _endpointBuilder = new PriorityEndpointSelectorBuilder(priorityOpenAiEndpointDispatcherBuilder,
            fallbackOpenAiEndpointDispatcherBuilder);

        return this;
    }


    public TestAICentralPipelineBuilder WithRandomEndpoints(
        (string hostname, string model, string mappedModel)[] endpoints)
    {
        _openAiEndpointDispatcherBuilders = endpoints.Select(x =>
            new OpenAIEndpointDispatcherBuilder($"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                AuthenticationType.ApiKey,
                Guid.NewGuid().ToString())).ToArray();

        _endpointBuilder = new RandomEndpointSelectorBuilder(_openAiEndpointDispatcherBuilders!);

        return this;
    }

    public AICentralPipelineAssembler Assemble(string path)
    {
        var id = Guid.NewGuid().ToString();
        return new AICentralPipelineAssembler(
            new Dictionary<string, Func<Dictionary<string, string>, IAICentralRouter>>()
            {
                ["PathMatch"] = SimplePathMatchRouter.BuildFromConfig
            },
            new Dictionary<string, IAICentralClientAuthBuilder>()
            {
                [id] = _auth!,
            },
            _openAiEndpointDispatcherBuilders!.ToDictionary(x => Guid.NewGuid().ToString(), x => x),
            new Dictionary<string, IAICentralEndpointSelectorBuilder>()
            {
                [id] = _endpointBuilder!
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
                    AuthProvider = id,
                    Steps = Array.Empty<string>(),
                    EndpointSelector = id
                }
            }
        );
    }
}