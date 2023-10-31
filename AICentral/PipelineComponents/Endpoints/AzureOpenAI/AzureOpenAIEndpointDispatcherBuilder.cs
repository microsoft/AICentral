using System.Net;
using AICentral.PipelineComponents.Endpoints.EndpointAuth;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace AICentral.PipelineComponents.Endpoints.AzureOpenAI;

public class AzureOpenAIEndpointDispatcherBuilder : IAiCentralEndpointDispatcherBuilder
{
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencyStrategy;
    private static readonly HttpStatusCode[] StatusCodesToRetry = { HttpStatusCode.TooManyRequests };

    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly string _languageUrl;
    private readonly string _modelName;


    public AzureOpenAIEndpointDispatcherBuilder(
        string languageUrl,
        string modelName,
        AuthenticationType authenticationType,
        string? authenticationKey)
    {
        Guid.NewGuid().ToString();
        _languageUrl = languageUrl;
        _modelName = modelName;

        _authHandler = authenticationType switch
        {
            AuthenticationType.ApiKey => new KeyAuth(authenticationKey ??
                                                                throw new ArgumentException(
                                                                    "Missing api-key for Authrntication Type")),
            AuthenticationType.Entra => new EntraAuth(),
            AuthenticationType.EntraPassThrough => new BearerTokenPassThrough(),
            _ => throw new ArgumentOutOfRangeException(nameof(authenticationType), authenticationType, null)
        };

        var handler = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>(e =>
                e.StatusCode.HasValue && StatusCodesToRetry.Contains(e.StatusCode.Value));

        _resiliencyStrategy = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = handler
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                Delay = TimeSpan.FromSeconds(0.2),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = 3,
                ShouldHandle = handler
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAIEndpointDispatcher, AIEndpointDispatcher>();
        services.AddHttpClient<HttpAIEndpointDispatcher>();
    }

    public static string ConfigName => "AzureOpenAIEndpoint";

    public static IAiCentralEndpointDispatcherBuilder BuildFromConfig(Dictionary<string, string> parameters)
    {
        return new AzureOpenAIEndpointDispatcherBuilder(
            parameters["LanguageEndpoint"],
            parameters["ModelName"],
            Enum.Parse<AuthenticationType>(parameters["AuthenticationType"]),
            parameters.TryGetValue("ApiKey", out var value) ? value : string.Empty);
    }

    public IAICentralEndpointDispatcher Build()
    {
        return new AzureOpenAIEndpointDispatcher(_languageUrl, _modelName, _authHandler, _resiliencyStrategy);
    }
}