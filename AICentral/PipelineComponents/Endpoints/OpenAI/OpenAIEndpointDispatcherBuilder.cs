using System.Net;
using AICentral.Configuration.JSON;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace AICentral.PipelineComponents.Endpoints.OpenAI;

public class OpenAIEndpointDispatcherBuilder : IAICentralEndpointDispatcherBuilder
{
    private static readonly HttpStatusCode[] StatusCodesToRetry = { HttpStatusCode.TooManyRequests };

    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly string _languageUrl;
    private readonly Dictionary<string, string> _modelMappings;

    public OpenAIEndpointDispatcherBuilder(
        string languageUrl,
        Dictionary<string, string> modelMappings,
        AuthenticationType authenticationType,
        string? authenticationKey)
    {
        _languageUrl = languageUrl;
        _modelMappings = modelMappings;

        _authHandler = authenticationType switch
        {
            AuthenticationType.ApiKey => new KeyAuth(authenticationKey ?? throw new ArgumentException("Missing api-key for Authentication Type")),
            AuthenticationType.Entra => new EntraAuth(),
            AuthenticationType.EntraPassThrough => new BearerTokenPassThroughAuth(),
            _ => throw new ArgumentOutOfRangeException(nameof(authenticationType), authenticationType, null)
        };
    }

    public void RegisterServices(IServiceCollection services)
    {
        var handler = new PredicateBuilder<HttpResponseMessage>()
            .HandleResult(r => StatusCodesToRetry.Contains(r.StatusCode))
            .Handle<HttpRequestException>(e =>
                e.StatusCode.HasValue && StatusCodesToRetry.Contains(e.StatusCode.Value));

        var resiliencyStrategy = new ResiliencePipelineBuilder<HttpResponseMessage>()
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

        services.AddHttpClient<HttpAIEndpointDispatcher>()
            .AddPolicyHandler(resiliencyStrategy.AsAsyncPolicy());
    }

    public static string ConfigName => "AzureOpenAIEndpoint";

    public static IAICentralEndpointDispatcherBuilder BuildFromConfig(IConfigurationSection configurationSection)
    {
        if (!configurationSection.Exists()) throw new ArgumentException($"Missing configuration section {configurationSection.Path}");
        var parameters = configurationSection.Get<ConfigurationTypes.AICentralPipelineEndpointPropertiesConfig>();
        
        return new OpenAIEndpointDispatcherBuilder(
            Guard.NotNull(parameters.LanguageEndpoint, configurationSection, nameof(parameters.LanguageEndpoint)),
            Guard.NotNull(parameters.ModelMappings, configurationSection, nameof(parameters.ModelMappings)),
            Guard.NotNull(parameters.AuthenticationType, configurationSection, nameof(parameters.AuthenticationType)),
            parameters.ApiKey);
    }

    public IAICentralEndpointDispatcher Build()
    {
        return new OpenAIEndpointDispatcher(_languageUrl, _modelMappings, _authHandler);
    }
}