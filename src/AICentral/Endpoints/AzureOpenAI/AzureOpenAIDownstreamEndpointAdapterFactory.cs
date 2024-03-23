using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI;

public class AzureOpenAIDownstreamEndpointAdapterFactory : IDownstreamEndpointAdapterFactory
{
    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly string _endpointName;
    private readonly string _languageUrl;
    private readonly Dictionary<string, string> _assistantMappings;
    private readonly bool _enforceMappedModels;
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _id;
    private readonly int? _maxConcurrency;
    
    public AzureOpenAIDownstreamEndpointAdapterFactory(
        string endpointName,
        string languageUrl,
        string authenticationType,
        string? authenticationKey,
        Dictionary<string, string> modelMappings,
        Dictionary<string, string> assistantMappings,
        bool enforceMappedModels = false,
        int? maxConcurrency = null)
    {
        _id = Guid.NewGuid().ToString();

        _endpointName = endpointName;
        _languageUrl = languageUrl;
        _modelMappings = modelMappings;
        _assistantMappings = assistantMappings;
        _enforceMappedModels = enforceMappedModels;
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

    }

    public void RegisterServices(HttpMessageHandler? httpMessageHandler, IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .AddPolicyHandler(ResiliencyStrategy.Build(_maxConcurrency))
            .ConfigurePrimaryHttpMessageHandler(() => httpMessageHandler ?? new HttpClientHandler());
    }

    public static string ConfigName => "AzureOpenAIEndpoint";

    public static IDownstreamEndpointAdapterFactory BuildFromConfig(
        ILogger logger,
        TypeAndNameConfig config)
    {
        var properties = config.TypedProperties<AzureOpenAIEndpointPropertiesConfig>();
        
        Guard.NotNull(properties, "Properties");

        var authenticationType = properties.AuthenticationType;

        if (authenticationType == null)
        {
            logger.LogWarning(
                "Pipeline {ConfigurationSectionPath} has no AuthType configured. Defaulting to AAD pass-through",
                config.Name);
            authenticationType = "EntraPassThrough";
        }

        return new AzureOpenAIDownstreamEndpointAdapterFactory(
            config.Name!,
            Guard.NotNull(properties.LanguageEndpoint, nameof(properties.LanguageEndpoint)),
            authenticationType,
            properties.ApiKey,
            properties.ModelMappings ?? new Dictionary<string, string>(),
            properties.AssistantMappings ?? new Dictionary<string, string>(),
            properties.EnforceMappedModels ?? false,
            properties.MaxConcurrency);
    }

    public IDownstreamEndpointAdapter Build()
    {
        return new AzureOpenAIDownstreamEndpointAdapter(_id, _languageUrl, _endpointName, _modelMappings, _assistantMappings, _authHandler, _enforceMappedModels);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "AzureOpenAI",
            Url = _languageUrl,
            Mappings = _modelMappings,
            AssistantMappings = _assistantMappings,
            Auth = _authHandler.WriteDebug()
        };
    }
}