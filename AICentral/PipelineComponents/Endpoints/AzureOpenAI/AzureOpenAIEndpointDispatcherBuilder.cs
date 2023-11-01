using System.Net;
using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Endpoints.EndpointAuth;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace AICentral.PipelineComponents.Endpoints.AzureOpenAI;

public class AzureOpenAIEndpointDispatcherBuilder : IAICentralEndpointDispatcherBuilder
{
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencyStrategy;
    private static readonly HttpStatusCode[] StatusCodesToRetry = { HttpStatusCode.TooManyRequests };

    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly string _languageUrl;
    private readonly Dictionary<string, string> _modelMappings;

    public AzureOpenAIEndpointDispatcherBuilder(
        string languageUrl,
        Dictionary<string, string> modelMappings,
        AuthenticationType authenticationType,
        string? authenticationKey)
    {
        _languageUrl = languageUrl;
        _modelMappings = modelMappings;

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

    public static IAICentralEndpointDispatcherBuilder BuildFromConfig(ConfigurationTypes.AICentralPipelineEndpointPropertiesConfig parameters)
    {
        return new AzureOpenAIEndpointDispatcherBuilder(
            parameters.LanguageEndpoint!,
            parameters.ModelMappings!,
            parameters.AuthenticationType,
            parameters.ApiKey);
    }

    public IAICentralEndpointDispatcher Build()
    {
        return new AzureOpenAIEndpointDispatcher(_languageUrl, _modelMappings, _authHandler, _resiliencyStrategy);
    }
}