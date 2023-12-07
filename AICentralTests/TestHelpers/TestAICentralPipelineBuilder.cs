using System.Threading.RateLimiting;
using AICentral;
using AICentral.Configuration.JSON;
using AICentral.Core;
using AICentral.Steps.Auth;
using AICentral.Steps.Auth.AllowAnonymous;
using AICentral.Steps.Auth.ApiKey;
using AICentral.Steps.BulkHead;
using AICentral.Steps.Endpoints;
using AICentral.Steps.Endpoints.OpenAILike.AzureOpenAI;
using AICentral.Steps.Endpoints.OpenAILike.OpenAI;
using AICentral.Steps.EndpointSelectors;
using AICentral.Steps.EndpointSelectors.LowestLatency;
using AICentral.Steps.EndpointSelectors.Priority;
using AICentral.Steps.EndpointSelectors.Random;
using AICentral.Steps.EndpointSelectors.Single;
using AICentral.Steps.RateLimiting;
using AICentral.Steps.Routes;

namespace AICentralTests.TestHelpers;

public class TestAICentralPipelineBuilder
{
    private IAICentralClientAuthFactory? _auth;
    private IAICentralEndpointSelectorFactory? _endpointFactory;
    private IAICentralEndpointDispatcherFactory[]? _openAiEndpointDispatcherBuilders;
    private int? _windowInSeconds;
    private int? _requestsPerWindow;
    private int? _allowedConcurrency;

    public TestAICentralPipelineBuilder WithApiKeyAuth(string key1, string key2)
    {
        _auth = new ApiKeyClientAuthFactory(
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
        _auth = new AllowAnonymousClientAuthFactory();
        return this;
    }

    public TestAICentralPipelineBuilder WithSingleEndpoint(string hostname, string model, string mappedModel,
        int? maxConcurrency = null)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIEndpointDispatcherFactory($"https://{hostname}",
            new Dictionary<string, string>()
            {
                [model] = mappedModel
            },
            AuthenticationType.ApiKey,
            Guid.NewGuid().ToString(),
            maxConcurrency);

        _endpointFactory = new SingleEndpointSelectorFactory(openAiEndpointDispatcherBuilder);
        _openAiEndpointDispatcherBuilders = new[] { openAiEndpointDispatcherBuilder };

        return this;
    }


    public TestAICentralPipelineBuilder WithSingleOpenAIEndpoint(string model, string mappedModel)
    {
        var openAiEndpointDispatcherBuilder = new OpenAIEndpointDispatcherFactory(
            new Dictionary<string, string>()
            {
                [model] = mappedModel
            },
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            null);

        _endpointFactory = new SingleEndpointSelectorFactory(openAiEndpointDispatcherBuilder);
        _openAiEndpointDispatcherBuilders = new[] { openAiEndpointDispatcherBuilder };

        return this;
    }

    public TestAICentralPipelineBuilder WithPriorityEndpoints(
        (string hostname, string model, string mappedModel)[] priorityEndpoints,
        (string hostname, string model, string mappedModel)[] fallbackEndpoints
    )
    {
        IAICentralEndpointDispatcherFactory[] priorityOpenAIEndpointDispatcherBuilder = priorityEndpoints.Select(x =>
            new AzureOpenAIEndpointDispatcherFactory($"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                AuthenticationType.ApiKey,
                Guid.NewGuid().ToString())).ToArray();

        IAICentralEndpointDispatcherFactory[] fallbackOpenAIEndpointDispatcherBuilder = fallbackEndpoints.Select(x =>
            new AzureOpenAIEndpointDispatcherFactory($"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                AuthenticationType.ApiKey,
                Guid.NewGuid().ToString())).ToArray();

        _openAiEndpointDispatcherBuilders = priorityOpenAIEndpointDispatcherBuilder
            .Union(fallbackOpenAIEndpointDispatcherBuilder).ToArray();

        _endpointFactory = new PriorityEndpointSelectorFactory(priorityOpenAIEndpointDispatcherBuilder,
            fallbackOpenAIEndpointDispatcherBuilder);

        return this;
    }


    public TestAICentralPipelineBuilder WithRandomEndpoints(
        params (string hostname, string model, string mappedModel)[] endpoints)
    {
        _openAiEndpointDispatcherBuilders = endpoints.Select(x =>
            new AzureOpenAIEndpointDispatcherFactory($"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                AuthenticationType.ApiKey,
                Guid.NewGuid().ToString())).ToArray();

        _endpointFactory = new RandomEndpointSelectorFactory(_openAiEndpointDispatcherBuilders!);

        return this;
    }

    public TestAICentralPipelineBuilder WithLowestLatencyEndpoints(
        params (string hostname, string model, string mappedModel)[] endpoints)
    {
        _openAiEndpointDispatcherBuilders = endpoints.Select(x =>
            new AzureOpenAIEndpointDispatcherFactory($"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                AuthenticationType.ApiKey,
                Guid.NewGuid().ToString())).ToArray();

        _endpointFactory = new LowestLatencyEndpointSelectorFactory(_openAiEndpointDispatcherBuilders!);
        return this;
    }

    public TestAICentralPipelineBuilder WithBulkHead(int maxConcurrency)
    {
        _allowedConcurrency = maxConcurrency;
        return this;
    }

    public AICentralPipelineAssembler Assemble(string host)
    {
        var id = Guid.NewGuid().ToString();
        var genericSteps = new Dictionary<string, IAICentralGenericStepFactory>();
        var steps = new List<string>();

        if (_windowInSeconds != null)
        {
            var stepId = Guid.NewGuid().ToString();
            genericSteps[stepId] = new FixedWindowRateLimitingProvider(new FixedWindowRateLimiterOptions()
            {
                Window = TimeSpan.FromSeconds(_windowInSeconds.Value),
                PermitLimit = _requestsPerWindow!.Value
            });
            steps.Add(stepId);
        }

        if (_allowedConcurrency != null)
        {
            var stepId = Guid.NewGuid().ToString();
            genericSteps[stepId] = new BulkHeadProviderFactory(new BulkHeadConfiguration()
                { MaxConcurrency = _allowedConcurrency });
            steps.Add(stepId);
        }

        return new AICentralPipelineAssembler(
            HeaderMatchRouter.WithHostHeader,
            new Dictionary<string, IAICentralClientAuthFactory>()
            {
                [id] = _auth ?? new AllowAnonymousClientAuthFactory(),
            },
            _openAiEndpointDispatcherBuilders!.ToDictionary(x => Guid.NewGuid().ToString(), x => x),
            new Dictionary<string, IAICentralEndpointSelectorFactory>()
            {
                [id] = _endpointFactory!
            },
            genericSteps,
            new[]
            {
                new ConfigurationTypes.AICentralPipelineConfig()
                {
                    Name = Guid.NewGuid().ToString(),
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

    public TestAICentralPipelineBuilder WithHierarchicalEndpointSelector(string endpoint200, string model, string mappedModel)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIEndpointDispatcherFactory(
            $"https://{endpoint200}",
            new Dictionary<string, string>()
            {
                [model] = mappedModel
            },
            AuthenticationType.ApiKey,
            Guid.NewGuid().ToString());

        var endpointFactory = new SingleEndpointSelectorFactory(openAiEndpointDispatcherBuilder);
        _endpointFactory = new SingleEndpointSelectorFactory(new EndpointSelectorAdapterFactory(endpointFactory));
        _openAiEndpointDispatcherBuilders = new[] { openAiEndpointDispatcherBuilder };

        return this;
    }
}