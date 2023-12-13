using AICentral.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AICentral.OpenAI.AzureOpenAI;

public class AzureOpenAIEndpointRequestResponseHandlerFactory : IEndpointRequestResponseHandlerFactory
{
    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly string _languageUrl;
    private readonly Dictionary<string, string> _modelMappings;
    private readonly Lazy<IEndpointRequestResponseHandler> _endpointDispatcher;
    private readonly string _id;
    private readonly int? _maxConcurrency;
    
    public AzureOpenAIEndpointRequestResponseHandlerFactory(
        string endpointName,
        string languageUrl,
        Dictionary<string, string> modelMappings,
        string authenticationType,
        string? authenticationKey,
        int? maxConcurrency = null)
    {
        _id = Guid.NewGuid().ToString();

        _languageUrl = languageUrl;
        _modelMappings = modelMappings;
        _maxConcurrency = maxConcurrency;

        _authHandler = authenticationType.ToLowerInvariant() switch
        {
            "apikey" => new KeyAuth(authenticationKey ??
                                                     throw new ArgumentException(
                                                         "Missing api-key for Authentication Type")),
            "entra" => new EntraAuth(),
            "entrapassthrough" => new BearerTokenPassThroughAuth(),
            _ => throw new ArgumentOutOfRangeException(nameof(authenticationType), authenticationType, null)
        };

        _endpointDispatcher = new Lazy<IEndpointRequestResponseHandler>(() =>
            new AzureOpenAIEndpointDispatcher(_id, _languageUrl, endpointName, _modelMappings, _authHandler));
    }

    public void RegisterServices(HttpMessageHandler? httpMessageHandler, IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .AddPolicyHandler(ResiliencyStrategy.Build(_maxConcurrency))
            .ConfigurePrimaryHttpMessageHandler(() => httpMessageHandler ?? new HttpClientHandler());
    }

    public static string ConfigName => "AzureOpenAIEndpoint";

    public static IEndpointRequestResponseHandlerFactory BuildFromConfig(
        ILogger logger,
        AICentralTypeAndNameConfig config)
    {
        var properties = config.TypedProperties<AICentralPipelineAzureOpenAIEndpointPropertiesConfig>();
        
        Guard.NotNull(properties, "Properties");

        var modelMappings = properties!.ModelMappings;
        var authenticationType = properties.AuthenticationType;
        if (modelMappings == null)
        {
            logger.LogWarning(
                "Endpoint {Name} has no model mappings configured. All requests will use default behaviour of passing model name straight through",
                config.Name);

            modelMappings = new Dictionary<string, string>();
        }

        if (authenticationType == null)
        {
            logger.LogWarning(
                "Pipeline {ConfigurationSectionPath} has no AuthType configured. Defaulting to AAD pass-through",
                config.Name);
            authenticationType = "EntraPassThrough";
        }

        return new AzureOpenAIEndpointRequestResponseHandlerFactory(
            config.Name!,
            Guard.NotNull(properties.LanguageEndpoint, nameof(properties.LanguageEndpoint)),
            modelMappings,
            authenticationType,
            properties.ApiKey,
            properties.MaxConcurrency);
    }

    public IAICentralEndpointDispatcher Build()
    {
        return new AICentralEndpointDispatcher(_endpointDispatcher.Value);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "AzureOpenAI",
            Url = _languageUrl,
            Mappings = _modelMappings,
            Auth = _authHandler.WriteDebug()
        };
    }
}