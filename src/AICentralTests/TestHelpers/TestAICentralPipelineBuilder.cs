using System.Threading.RateLimiting;
using AICentral;
using AICentral.Affinity;
using AICentral.BulkHead;
using AICentral.Configuration;
using AICentral.ConsumerAuth;
using AICentral.ConsumerAuth.AllowAnonymous;
using AICentral.ConsumerAuth.ApiKey;
using AICentral.Core;
using AICentral.Endpoints;
using AICentral.Endpoints.AzureOpenAI;
using AICentral.Endpoints.OpenAI;
using AICentral.EndpointSelectors;
using AICentral.EndpointSelectors.LowestLatency;
using AICentral.EndpointSelectors.Priority;
using AICentral.EndpointSelectors.Random;
using AICentral.EndpointSelectors.Single;
using AICentral.RateLimiting;
using FixedWindowRateLimiterOptions = AICentral.RateLimiting.FixedWindowRateLimiterOptions;

namespace AICentralTests.TestHelpers;

public class TestAICentralPipelineBuilder
{
    private IPipelineStepFactory? _auth;
    private IEndpointSelectorFactory? _endpointFactory;
    private IEndpointDispatcherFactory[]? _openAiEndpointDispatcherBuilders;
    private int? _windowInSeconds;
    private int? _requestsPerWindow;
    private int? _tokensPerWindow;
    private int? _allowedConcurrency;
    private RateLimitingLimitType? _fixedWindowLimitType;
    private RateLimitingLimitType? _tokenLimitType;
    private TimeSpan? _endpointAffinityTimespan;

    public TestAICentralPipelineBuilder WithApiKeyAuth(params (string clientName, string key1, string key2)[] clients)
    {
        _auth = new ApiKeyClientAuthFactory(
            new ApiKeyClientAuthConfig()
            {
                Clients = clients.Select(x =>
                    new ApiKeyClientAuthClientConfig()
                    {
                        ClientName = x.clientName,
                        Key1 = x.key1,
                        Key2 = x.key2
                    }).ToArray()
            });
        return this;
    }

    public TestAICentralPipelineBuilder WithNoAuth()
    {
        _auth = new AllowAnonymousClientAuthFactory();
        return this;
    }

    public TestAICentralPipelineBuilder WithSingleEndpoint(string hostname, int? maxConcurrencyToAllowThrough = null)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIDownstreamEndpointAdapterFactory(
            hostname,
            $"https://{hostname}",
            "ApiKey",
            "80a59060-63f8-4a19-a5ce-ad1a44157897",
            new Dictionary<string, string>(),
            maxConcurrencyToAllowThrough);

        _endpointFactory =
            new SingleEndpointSelectorFactory(new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder));
        _openAiEndpointDispatcherBuilders = new[]
            { new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder) };

        return this;
    }
    
    public TestAICentralPipelineBuilder WithRandomOpenAIEndpoints(
        (string name, string apiKey, string model, string mappedModel)[] endpoints)
    {
        _openAiEndpointDispatcherBuilders = endpoints.Select(x =>
            new DownstreamEndpointDispatcherFactory(
                new OpenAIDownstreamEndpointAdapterFactory(
                    x.name,
                    new Dictionary<string, string>()
                    {
                        [x.model] = x.mappedModel
                    },
                    new Dictionary<string, string>(),
                    x.apiKey,
                    "98892683-5712-4db4-ab5e-727275f88250", null))).Cast<IEndpointDispatcherFactory>().ToArray();

        _endpointFactory = new RandomEndpointSelectorFactory(_openAiEndpointDispatcherBuilders);

        return this;
    }

    public TestAICentralPipelineBuilder WithPriorityEndpoints(
        (string hostname, string model, string mappedModel)[] priorityEndpoints,
        (string hostname, string model, string mappedModel)[] fallbackEndpoints
    )
    {
        IEndpointDispatcherFactory[] priorityOpenAIEndpointDispatcherBuilder = priorityEndpoints.Select(x =>
            new DownstreamEndpointDispatcherFactory(new AzureOpenAIDownstreamEndpointAdapterFactory(
                x.hostname,
                $"https://{x.hostname}",
                "ApiKey",
                Guid.NewGuid().ToString(),
                new Dictionary<string, string>()
                ))).ToArray();

        IEndpointDispatcherFactory[] fallbackOpenAIEndpointDispatcherBuilder = fallbackEndpoints.Select(x =>
            new DownstreamEndpointDispatcherFactory(new AzureOpenAIDownstreamEndpointAdapterFactory(
                x.hostname,
                $"https://{x.hostname}",
                "ApiKey",
                Guid.NewGuid().ToString(),
                new Dictionary<string, string>()))).ToArray();

        _openAiEndpointDispatcherBuilders = priorityOpenAIEndpointDispatcherBuilder
            .Union(fallbackOpenAIEndpointDispatcherBuilder).ToArray();

        _endpointFactory = new PriorityEndpointSelectorFactory(
            priorityOpenAIEndpointDispatcherBuilder,
            fallbackOpenAIEndpointDispatcherBuilder);

        return this;
    }


    public TestAICentralPipelineBuilder WithRandomEndpoints(
        params (string hostname, string assistant, string mappedAssistant)[] endpoints)
    {
        _openAiEndpointDispatcherBuilders = endpoints.Select(x =>
            new DownstreamEndpointDispatcherFactory(new AzureOpenAIDownstreamEndpointAdapterFactory(
                x.hostname,
                $"https://{x.hostname}",
                "ApiKey",
                "17f9b7db-f6b7-4b15-a868-38e19bbd88d1",
                new Dictionary<string, string>()
                {
                    [x.assistant] = x.mappedAssistant
                }))).ToArray();

        _endpointFactory = new RandomEndpointSelectorFactory(_openAiEndpointDispatcherBuilders!);

        return this;
    }

    public TestAICentralPipelineBuilder WithLowestLatencyEndpoints(
        params (string hostname, string model, string mappedModel)[] endpoints)
    {
        _openAiEndpointDispatcherBuilders = endpoints.Select(x =>
            new DownstreamEndpointDispatcherFactory(new AzureOpenAIDownstreamEndpointAdapterFactory(
                x.hostname,
                $"https://{x.hostname}",
                "ApiKey",
                Guid.NewGuid().ToString(),
                new Dictionary<string, string>()))).ToArray();

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
        var genericSteps = new Dictionary<string, IPipelineStepFactory>();
        var steps = new List<string>();

        if (_windowInSeconds != null && _requestsPerWindow != null)
        {
            var stepId = Guid.NewGuid().ToString();
            genericSteps[stepId] = new FixedWindowRateLimitingProvider(new FixedWindowRateLimiterOptions()
            {
                LimitType = _fixedWindowLimitType,
                Options = new System.Threading.RateLimiting.FixedWindowRateLimiterOptions()
                {
                    Window = TimeSpan.FromSeconds(_windowInSeconds.Value),
                    PermitLimit = _requestsPerWindow!.Value
                }
            });
            steps.Add(stepId);
        }

        if (_windowInSeconds != null && _tokensPerWindow != null)
        {
            var stepId = Guid.NewGuid().ToString();
            genericSteps[stepId] = new FixedWindowRateLimitingProvider(new FixedWindowRateLimiterOptions()
            {
                LimitType = _tokenLimitType,
                MetricType = RateLimitingMetricType.Tokens,
                Options = new System.Threading.RateLimiting.FixedWindowRateLimiterOptions()
                {
                    Window = TimeSpan.FromSeconds(_windowInSeconds.Value),
                    PermitLimit = _tokensPerWindow!.Value
                }
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

        if (_endpointAffinityTimespan != null)
        {
            var stepId = Guid.NewGuid().ToString();
            genericSteps[stepId] = new SingleNodeAffinityFactory(_endpointAffinityTimespan.Value);
            steps.Add(stepId);
        }
        
        return new AICentralPipelineAssembler(
            HostNameMatchRouter.WithHostHeader,
            new Dictionary<string, IPipelineStepFactory>()
            {
                [id] = _auth ?? new AllowAnonymousClientAuthFactory(),
            },
            _openAiEndpointDispatcherBuilders!.ToDictionary(x => Guid.NewGuid().ToString(), x => x),
            new Dictionary<string, IEndpointSelectorFactory>()
            {
                [id] = _endpointFactory!
            },
            genericSteps,
            new[]
            {
                new PipelineConfig()
                {
                    Name = host + "-pipeline",
                    Host = host,
                    AuthProvider = id,
                    Steps = steps.ToArray(),
                    EndpointSelector = id
                }
            }
        );
    }

    public TestAICentralPipelineBuilder WithRateLimiting(int windowInSeconds, int requestsPerWindow,
        RateLimitingLimitType? limitType = RateLimitingLimitType.PerAICentralEndpoint)
    {
        _fixedWindowLimitType = limitType;
        _requestsPerWindow = requestsPerWindow;
        _windowInSeconds = windowInSeconds;
        return this;
    }

    public TestAICentralPipelineBuilder WithTokenRateLimiting(int windowSize, int completionTokensPerWindow,
        RateLimitingLimitType? limitType = RateLimitingLimitType.PerAICentralEndpoint)
    {
        _windowInSeconds = windowSize;
        _tokensPerWindow = completionTokensPerWindow;
        _tokenLimitType = limitType;
        return this;
    }

    public TestAICentralPipelineBuilder WithHierarchicalEndpointSelector(string endpoint200, string model,
        string mappedModel)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIDownstreamEndpointAdapterFactory(
            endpoint200,
            $"https://{endpoint200}",
            "ApiKey",
            "bacca18e-f471-4eca-9ea3-c8ee7155dacb",
            new Dictionary<string, string>());

        var endpointFactory =
            new SingleEndpointSelectorFactory(new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder));
        _endpointFactory =
            new SingleEndpointSelectorFactory(new EndpointSelectorAdapterDispatcherFactory(endpointFactory));
        _openAiEndpointDispatcherBuilders = new[]
            { new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder) };

        return this;
    }

    public TestAICentralPipelineBuilder WithEndpointAffinity(TimeSpan affinityTimespan)
    {
        _endpointAffinityTimespan = affinityTimespan;
        return this;
    }
}