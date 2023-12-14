using System.Threading.RateLimiting;
using AICentral.Auth;
using AICentral.Auth.AllowAnonymous;
using AICentral.Auth.ApiKey;
using AICentral.BulkHead;
using AICentral.Configuration;
using AICentral.Core;
using AICentral.Endpoints;
using AICentral.EndpointSelectors;
using AICentral.EndpointSelectors.LowestLatency;
using AICentral.EndpointSelectors.Priority;
using AICentral.EndpointSelectors.Random;
using AICentral.EndpointSelectors.Single;
using AICentral.OpenAI.AzureOpenAI;
using AICentral.OpenAI.OpenAI;
using AICentral.RateLimiting;
using AICentral.Routes;

namespace AICentralTests.TestHelpers;

public class TestAICentralPipelineBuilder
{
    private IAICentralClientAuthFactory? _auth;
    private IAICentralEndpointSelectorFactory? _endpointFactory;
    private IAICentralEndpointDispatcherFactory[]? _openAiEndpointDispatcherBuilders;
    private int? _windowInSeconds;
    private int? _requestsPerWindow;
    private int? _allowedConcurrency;
    private RateLimitingLimitType? _fixedWindowLimitType;
    private int? _tokenWindowSize;
    private int? _tokensPerWindow;
    private RateLimitingLimitType? _tokenLimitType;

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

    public TestAICentralPipelineBuilder WithSingleEndpoint(string hostname, string model, string mappedModel,
        int? maxConcurrency = null)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIEndpointRequestResponseHandlerFactory(
            hostname,
            $"https://{hostname}",
            new Dictionary<string, string>()
            {
                [model] = mappedModel
            },
            "ApiKey",
            Guid.NewGuid().ToString());

        _endpointFactory = new SingleEndpointSelectorFactory(new AICentralEndpointDispatcherFactory(openAiEndpointDispatcherBuilder));
        _openAiEndpointDispatcherBuilders = new[] { new AICentralEndpointDispatcherFactory(openAiEndpointDispatcherBuilder) };

        return this;
    }


    public TestAICentralPipelineBuilder WithSingleOpenAIEndpoint(string name, string model, string mappedModel)
    {
        var openAiEndpointDispatcherBuilder = new OpenAIEndpointRequestResponseHandlerFactory(
            name,
            new Dictionary<string, string>()
            {
                [model] = mappedModel
            },
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());

        _endpointFactory = new SingleEndpointSelectorFactory(new AICentralEndpointDispatcherFactory(openAiEndpointDispatcherBuilder));
        _openAiEndpointDispatcherBuilders = new[] { new AICentralEndpointDispatcherFactory(openAiEndpointDispatcherBuilder) };

        return this;
    }

    public TestAICentralPipelineBuilder WithPriorityEndpoints(
        (string hostname, string model, string mappedModel)[] priorityEndpoints,
        (string hostname, string model, string mappedModel)[] fallbackEndpoints
    )
    {
        IAICentralEndpointDispatcherFactory[] priorityOpenAIEndpointDispatcherBuilder = priorityEndpoints.Select(x =>
            new AICentralEndpointDispatcherFactory(new AzureOpenAIEndpointRequestResponseHandlerFactory(
                x.hostname,
                $"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                "ApiKey",
                Guid.NewGuid().ToString()))).ToArray();

        IAICentralEndpointDispatcherFactory[] fallbackOpenAIEndpointDispatcherBuilder = fallbackEndpoints.Select(x =>
            new AICentralEndpointDispatcherFactory(new AzureOpenAIEndpointRequestResponseHandlerFactory(
                x.hostname,
                $"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                "ApiKey",
                Guid.NewGuid().ToString()))).ToArray();

        _openAiEndpointDispatcherBuilders = priorityOpenAIEndpointDispatcherBuilder
            .Union(fallbackOpenAIEndpointDispatcherBuilder).ToArray();

        _endpointFactory = new PriorityEndpointSelectorFactory(
            priorityOpenAIEndpointDispatcherBuilder,
            fallbackOpenAIEndpointDispatcherBuilder);

        return this;
    }


    public TestAICentralPipelineBuilder WithRandomEndpoints(
        params (string hostname, string model, string mappedModel)[] endpoints)
    {
        _openAiEndpointDispatcherBuilders = endpoints.Select(x =>
            new AICentralEndpointDispatcherFactory(new AzureOpenAIEndpointRequestResponseHandlerFactory(
                x.hostname,
                $"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                "ApiKey",
                Guid.NewGuid().ToString()))).ToArray();

        _endpointFactory = new RandomEndpointSelectorFactory(_openAiEndpointDispatcherBuilders!);

        return this;
    }

    public TestAICentralPipelineBuilder WithLowestLatencyEndpoints(
        params (string hostname, string model, string mappedModel)[] endpoints)
    {
        _openAiEndpointDispatcherBuilders = endpoints.Select(x =>
            new AICentralEndpointDispatcherFactory(new AzureOpenAIEndpointRequestResponseHandlerFactory(
                x.hostname,
                $"https://{x.hostname}", new Dictionary<string, string>()
                {
                    [x.model] = x.mappedModel
                },
                "ApiKey",
                Guid.NewGuid().ToString()))).ToArray();

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
            genericSteps[stepId] = new FixedWindowRateLimitingProvider(new AICentralFixedWindowRateLimiterOptions()
            {
                LimitType = _fixedWindowLimitType,
                Options = new FixedWindowRateLimiterOptions()
                {
                    Window = TimeSpan.FromSeconds(_windowInSeconds.Value),
                    PermitLimit = _requestsPerWindow!.Value
                }
            });
            steps.Add(stepId);
        }

        if (_tokenWindowSize != null)
        {
            var stepId = Guid.NewGuid().ToString();
            genericSteps[stepId] = new TokenBasedRateLimitingProvider(new TokenBasedRateLimiterOptions()
            {
                LimitType = _tokenLimitType,
                PermitLimit = _tokensPerWindow,
                Window = _tokenWindowSize
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
                new AICentralPipelineConfig()
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
        _tokenWindowSize = windowSize;
        _tokensPerWindow = completionTokensPerWindow;
        _tokenLimitType = limitType;
        return this;
    }

    public TestAICentralPipelineBuilder WithHierarchicalEndpointSelector(string endpoint200, string model,
        string mappedModel)
    {
        var openAiEndpointDispatcherBuilder = new AzureOpenAIEndpointRequestResponseHandlerFactory(
            endpoint200,
            $"https://{endpoint200}",
            new Dictionary<string, string>()
            {
                [model] = mappedModel
            },
            "ApiKey",
            Guid.NewGuid().ToString());

        var endpointFactory = new SingleEndpointSelectorFactory(new AICentralEndpointDispatcherFactory(openAiEndpointDispatcherBuilder));
        _endpointFactory = new SingleEndpointSelectorFactory(new EndpointSelectorAdapterFactory(endpointFactory));
        _openAiEndpointDispatcherBuilders = new[] { new AICentralEndpointDispatcherFactory(openAiEndpointDispatcherBuilder) };

        return this;
    }
}