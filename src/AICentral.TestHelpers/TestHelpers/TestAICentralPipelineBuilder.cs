using System.Security.Claims;
using System.Security.Cryptography;
using AICentral;
using AICentral.Affinity;
using AICentral.AzureAISearchVectorizer;
using AICentral.BulkHead;
using AICentral.Configuration;
using AICentral.ConsumerAuth.AICentralJWT;
using AICentral.ConsumerAuth.AllowAnonymous;
using AICentral.ConsumerAuth.ApiKey;
using AICentral.ConsumerAuth.Entra;
using AICentral.Core;
using AICentral.Endpoints;
using AICentral.Endpoints.AzureOpenAI;
using AICentral.Endpoints.AzureOpenAI.Authorisers;
using AICentral.Endpoints.AzureOpenAI.Authorisers.BearerPassThroughWithAdditionalKey;
using AICentral.Endpoints.OpenAI;
using AICentral.EndpointSelectors.HighestCapacity;
using AICentral.EndpointSelectors.LowestLatency;
using AICentral.EndpointSelectors.Priority;
using AICentral.EndpointSelectors.Random;
using AICentral.EndpointSelectors.Single;
using AICentral.RateLimiting;
using AICentral.RequestFiltering;
using AICentralTests.TestHelpers.FakeIdp;
using Microsoft.Identity.Web;
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
    private static readonly DiagnosticsCollectorFactory DiagnosticsCollectorFactory = new();
    private static readonly string DiagnosticsCollectorFactoryId = Guid.NewGuid().ToString();
    private string[]? _allowedChatImageHostNames;
    private AzureAISearchVectorizerProxy? _routeProxy;

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

    public TestAICentralPipelineBuilder WithCustomJwtProvider(string jwtProviderStepName, Dictionary<string, string[]> pipelines)
    {
        var rsa = RSA.Create();
        _auth = new AICentralJwtAuthFactory(
            jwtProviderStepName,
            new AICentralJwtAuthProviderConfig()
            {
                AdminKey = "fake-admin-key",
                TokenIssuer = "fake-issuer",
                ValidPipelines = pipelines,
                PrivateKeyPem = rsa.ExportRSAPrivateKeyPem(),
                PublicKeyPem = rsa.ExportRSAPublicKeyPem()
            });
        return this;
    }

    public TestAICentralPipelineBuilder WithNoAuth()
    {
        _auth = new AllowAnonymousClientAuthFactory();
        return this;
    }

    public TestAICentralPipelineBuilder WithSingleEndpoint(
        string hostname, 
        int? maxConcurrencyToAllowThrough = null,
        bool autoPopulateUser = false)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIDownstreamEndpointAdapterFactory(
            hostname,
            $"https://{hostname}",
            new KeyAuthFactory("ignore-fake-key-hr987345"),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            enforceMappedModels: false,
            maxConcurrencyToAllowThrough,
            autoPopulateUser);

        _endpointFactory =
            new SingleEndpointSelectorFactory(new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder));
        _openAiEndpointDispatcherBuilders = new[]
            { new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder) };

        return this;
    }

    public TestAICentralPipelineBuilder WithAzureAISearchRouteProxy()
    {
        _routeProxy = new AzureAISearchVectorizerProxy("/proxypath", "embeddings", "2024-04-01-preview");
        return this;
    }

    public TestAICentralPipelineBuilder WithSingleEndpointBearerPlusKey(
        string hostname)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIDownstreamEndpointAdapterFactory(
            hostname,
            $"https://{hostname}",
            new BearerPassThroughWithAdditionalKeyAuthFactory(new BearerPassThroughWithAdditionalKeyAuthFactoryConfig()
            {
                IncomingClaimName = ClaimTypes.Name,
                KeyHeaderName = "new-api-key",
                ClaimsToKeys = [
                    new ClaimValueToSubscriptionKey {ClaimValues = ["user1"], SubscriptionKey = "key-1"},
                    new ClaimValueToSubscriptionKey {ClaimValues = ["user2"], SubscriptionKey = "key-2"},
                ]
            }),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            enforceMappedModels: false);

        _endpointFactory =
            new SingleEndpointSelectorFactory(new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder));
        _openAiEndpointDispatcherBuilders = new IEndpointDispatcherFactory[]
            { new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder) };

        return this;
    }

    public TestAICentralPipelineBuilder WithSingleMappedEndpoint(string hostname, string model, string mappedModel, bool enforceMappedModels = false)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIDownstreamEndpointAdapterFactory(
            hostname,
            $"https://{hostname}",
            new KeyAuthFactory("ignore-fake-key-hr987345"),
            new Dictionary<string, string>()
            {
                [model] = mappedModel
            },
            new Dictionary<string, string>(),
            enforceMappedModels);

        _endpointFactory =
            new SingleEndpointSelectorFactory(new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder));
        _openAiEndpointDispatcherBuilders = new[]
            { new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder) };

        return this;
    }

    public TestAICentralPipelineBuilder WithMappedEndpoints((string hostname, (string model, string mappedModel)[] models)[] endpoints, bool enforceMappedModels = false)
    {
        var builtEndpoints = endpoints.Select(x => 
            new AzureOpenAIDownstreamEndpointAdapterFactory(
                x.hostname,
                $"https://{x.hostname}",
                new KeyAuthFactory("ignore-fake-key-hr987345"),
                x.models.ToDictionary(mm => mm.model, mm => mm.mappedModel),
                new Dictionary<string, string>(),
                enforceMappedModels,
                logMissingModelMappingsAsInformation: true)).ToArray();

        var endpointDispatcherFactories = builtEndpoints.Select(x => new DownstreamEndpointDispatcherFactory(x))
            .ToArray<IEndpointDispatcherFactory>();
        
        _endpointFactory = new RandomEndpointSelectorFactory(endpointDispatcherFactories);
            
        _openAiEndpointDispatcherBuilders = endpointDispatcherFactories;

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
                new KeyAuthFactory("ignore-fake-key-456456"),
                new Dictionary<string, string>(),
                new Dictionary<string, string>()
            ))).ToArray();

        IEndpointDispatcherFactory[] fallbackOpenAIEndpointDispatcherBuilder = fallbackEndpoints.Select(x =>
            new DownstreamEndpointDispatcherFactory(new AzureOpenAIDownstreamEndpointAdapterFactory(
                x.hostname,
                $"https://{x.hostname}",
                new KeyAuthFactory("ignore-fake-key-sdfsdf"),
                new Dictionary<string, string>(),
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
                new KeyAuthFactory("ignore-fake-key-12345678"),
                new Dictionary<string, string>(),
                new Dictionary<string, string>()
                {
                    [x.assistant] = x.mappedAssistant
                }))).ToArray();

        _endpointFactory = new RandomEndpointSelectorFactory(_openAiEndpointDispatcherBuilders!);

        return this;
    }

    public TestAICentralPipelineBuilder WithMetricsBasedEndpoints(
        params (string hostname, string assistant, string mappedAssistant)[] endpoints)
    {
        _openAiEndpointDispatcherBuilders = endpoints.Select(x =>
            new DownstreamEndpointDispatcherFactory(new AzureOpenAIDownstreamEndpointAdapterFactory(
                x.hostname,
                $"https://{x.hostname}",
                new KeyAuthFactory("ignore-fake-key-12345678"),
                new Dictionary<string, string>(),
                new Dictionary<string, string>()
                {
                    [x.assistant] = x.mappedAssistant
                }))).ToArray();

        _endpointFactory = new HighestCapacitySelectorFactory(_openAiEndpointDispatcherBuilders!);

        return this;
    }

    public TestAICentralPipelineBuilder WithLowestLatencyEndpoints(
        params (string hostname, string model, string mappedModel)[] endpoints)
    {
        _openAiEndpointDispatcherBuilders = endpoints.Select(x =>
            new DownstreamEndpointDispatcherFactory(new AzureOpenAIDownstreamEndpointAdapterFactory(
                x.hostname,
                $"https://{x.hostname}",
                new KeyAuthFactory("fake-dfjiud"),
                new Dictionary<string, string>(),
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
        var routeProxies = new Dictionary<string, IRouteProxy>();
        
        var steps = new List<string>();
        var proxies = new List<string>();

        genericSteps[DiagnosticsCollectorFactoryId] = DiagnosticsCollectorFactory;
        steps.Add(DiagnosticsCollectorFactoryId);

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

        if (_allowedChatImageHostNames != null)
        {
            var stepId = Guid.NewGuid().ToString();
            genericSteps[stepId] = new RequestFilteringProviderFactory(new RequestFilteringConfiguration()
            {
                AllowedHostNames = _allowedChatImageHostNames,
                AllowDataUris = false
            });
            steps.Add(stepId);
        }

        if (_endpointAffinityTimespan != null)
        {
            var stepId = Guid.NewGuid().ToString();
            genericSteps[stepId] = new SingleNodeAffinityFactory(_endpointAffinityTimespan.Value);
            steps.Add(stepId);
        }

        if (_routeProxy != null)
        {
            var stepId = Guid.NewGuid().ToString();
            routeProxies.Add(stepId, _routeProxy);
            proxies.Add(stepId);
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
            routeProxies,
            new[]
            {
                new PipelineConfig()
                {
                    Name = host + "-pipeline",
                    Host = host,
                    AuthProvider = id,
                    Steps = steps.ToArray(),
                    RouteProxies = proxies.ToArray(),
                    EndpointSelector = id
                }
            },
            true
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
    
    public TestAICentralPipelineBuilder WithChatFiltering()
    {
        _allowedChatImageHostNames = ["somewheregood.com"];
        return this;
    }

    public TestAICentralPipelineBuilder WithHierarchicalEndpointSelector(string endpoint200, string model,
        string mappedModel)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIDownstreamEndpointAdapterFactory(
            endpoint200,
            $"https://{endpoint200}",
            new KeyAuthFactory("fake-key-23324"),
            new Dictionary<string, string>(),
            new Dictionary<string, string>());

        var endpointFactory =
            new SingleEndpointSelectorFactory(new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder));
        _endpointFactory =
            new SingleEndpointSelectorFactory(new EndpointSelectorAdapterDispatcherFactory(endpointFactory));
        _openAiEndpointDispatcherBuilders = [new DownstreamEndpointDispatcherFactory(openAiEndpointDispatcherBuilder)];

        return this;
    }

    public TestAICentralPipelineBuilder WithEndpointAffinity(TimeSpan affinityTimespan)
    {
        _endpointAffinityTimespan = affinityTimespan;
        return this;
    }

    public TestAICentralPipelineBuilder WithFakeEntraClientAuth()
    {
        _auth = new EntraClientAuthFactory(
            new EntraClientAuthConfig(),
            (builder, id) =>
                builder.AddMicrosoftIdentityWebApi(options =>
                {
                    options.Audience = "https://cognitiveservices.azure.com";

                    //Test code... There's a bit inside Microsoft.Identity.Web that doesn't use the Backchannel
                    //handler for fetching a discovery document. 
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.BackchannelHttpHandler = new FakeIdpMessageHandler();
                }, options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.ClientId = "ignored-as-not-exchaning-codes-for-tokens";
                    options.TenantId = FakeIdpMessageHandler.TenantId;
                    options.BackchannelHttpHandler = new FakeIdpMessageHandler();
                }, id));
        
        return this;
    }
}