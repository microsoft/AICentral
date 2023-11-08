using System.Threading.RateLimiting;
using AICentral;
using AICentral.Configuration.JSON;
using AICentral.Steps;
using AICentral.Steps.Auth;
using AICentral.Steps.Auth.AllowAnonymous;
using AICentral.Steps.Auth.ApiKey;
using AICentral.Steps.Endpoints;
using AICentral.Steps.Endpoints.OpenAILike.AzureOpenAI;
using AICentral.Steps.Endpoints.OpenAILike.OpenAI;
using AICentral.Steps.EndpointSelectors;
using AICentral.Steps.EndpointSelectors.Priority;
using AICentral.Steps.EndpointSelectors.Random;
using AICentral.Steps.EndpointSelectors.Single;
using AICentral.Steps.RateLimiting;
using AICentral.Steps.Routes;
using Microsoft.Extensions.Configuration;

namespace AICentralTests.TestHelpers;

public class TestAICentralPipelineBuilder
{
    private IAICentralClientAuthBuilder? _auth;
    private IAICentralEndpointSelectorBuilder? _endpointBuilder;
    private IAICentralEndpointDispatcherBuilder[]? _openAiEndpointDispatcherBuilders;
    private EndpointType? _endpointType;
    private int? _windowInSeconds;
    private int? _requestsPerWindow;

    public TestAICentralPipelineBuilder WithApiKeyAuth(string key1, string key2)
    {
        _auth = new ApiKeyClientAuthBuilder(
            new ConfigurationTypes.ApiKeyClientAuthConfig()
            {
                Clients = new[]
                {
                    new ConfigurationTypes.ApiKeyClientAuthClientConfig()
                    {
                        ClientName = "test-client",
                        Key1 = key1,
                        Key2 = key2
                    }
                }
            });
        return this;
    }

    public TestAICentralPipelineBuilder WithNoAuth()
    {
        _auth = new AllowAnonymousClientAuthBuilder();
        return this;
    }

    public TestAICentralPipelineBuilder WithEndpointType(EndpointType endpointType)
    {
        _endpointType = endpointType;
        return this;
    }

    public TestAICentralPipelineBuilder WithSingleEndpoint(string hostname, string model, string mappedModel)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIEndpointDispatcherBuilder($"https://{hostname}",
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


    public TestAICentralPipelineBuilder WithSingleOpenAIEndpoint(string model, string mappedModel)
    {
        var openAiEndpointDispatcherBuilder = new OpenAIEndpointDispatcherBuilder(
            new Dictionary<string, string>()
            {
                [model] = mappedModel
            },
            Guid.NewGuid().ToString(),
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
            new AzureOpenAIEndpointDispatcherBuilder($"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                AuthenticationType.ApiKey,
                Guid.NewGuid().ToString())).ToArray();

        IAICentralEndpointDispatcherBuilder[] fallbackOpenAiEndpointDispatcherBuilder = fallbackEndpoints.Select(x =>
            new AzureOpenAIEndpointDispatcherBuilder($"https://{x.hostname}", new Dictionary<string, string>()
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
            new AzureOpenAIEndpointDispatcherBuilder($"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                AuthenticationType.ApiKey,
                Guid.NewGuid().ToString())).ToArray();

        _endpointBuilder = new RandomEndpointSelectorBuilder(_openAiEndpointDispatcherBuilders!);

        return this;
    }

    public AICentralPipelineAssembler Assemble(string host)
    {
        var id = Guid.NewGuid().ToString();
        var genericSteps = new Dictionary<string, IAICentralPipelineStepBuilder<IAICentralPipelineStep>>();
        var steps = new List<string>();
        if (_windowInSeconds != null)
        {
            genericSteps[id] = new FixedWindowRateLimitingProvider(new FixedWindowRateLimiterOptions()
            {
                Window = TimeSpan.FromSeconds(_windowInSeconds.Value),
                PermitLimit = _requestsPerWindow!.Value
            });
            steps.Add(id);
        }

        return new AICentralPipelineAssembler(
            HeaderMatchRouter.WithHostHeader,
            new Dictionary<string, IAICentralClientAuthBuilder>()
            {
                [id] = _auth ?? new AllowAnonymousClientAuthBuilder(),
            },
            _openAiEndpointDispatcherBuilders!.ToDictionary(x => Guid.NewGuid().ToString(), x => x),
            new Dictionary<string, IAICentralEndpointSelectorBuilder>()
            {
                [id] = _endpointBuilder!
            },
            genericSteps,
            new[]
            {
                new ConfigurationTypes.AICentralPipelineConfig()
                {
                    Name = Guid.NewGuid().ToString(),
                    EndpointType = _endpointType ?? EndpointType.AzureOpenAI,
                    Host = host,
                    AuthProvider = id,
                    Steps = steps.ToArray(),
                    EndpointSelector = id
                }
            }
        );
    }

    public TestAICentralPipelineBuilder WithRateLimiting(int windowInSeconds, int requestsPerWindow)
    {
        _requestsPerWindow = requestsPerWindow;
        _windowInSeconds = windowInSeconds;
        return this;
    }
}