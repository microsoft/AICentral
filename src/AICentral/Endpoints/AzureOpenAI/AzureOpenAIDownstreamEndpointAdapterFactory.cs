using AICentral.Core;
using AICentral.Endpoints.AzureOpenAI.Authorisers;

namespace AICentral.Endpoints.AzureOpenAI;

public class AzureOpenAIDownstreamEndpointAdapterFactory : IDownstreamEndpointAdapterFactory
{
    private readonly string _endpointName;
    private readonly string _languageUrl;
    private readonly IEndpointAuthorisationHandlerFactory _authorisationHandlerFactory;
    private readonly Dictionary<string, string> _assistantMappings;
    private readonly bool _enforceMappedModels;
    private readonly bool _logMissingModelMappingsAsInformation;
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _id;
    private readonly int? _maxConcurrency;
    private readonly bool _autoPopulateEmptyUserId;

    public AzureOpenAIDownstreamEndpointAdapterFactory(
        string endpointName,
        string languageUrl,
        IEndpointAuthorisationHandlerFactory authorisationHandlerFactory,
        Dictionary<string, string> modelMappings,
        Dictionary<string, string> assistantMappings,
        bool enforceMappedModels = false,
        int? maxConcurrency = null,
        bool autoPopulateEmptyUserId = false,
        bool logMissingModelMappingsAsInformation = false)
    {
        _id = Guid.NewGuid().ToString();

        _endpointName = endpointName;
        _languageUrl = languageUrl;
        _authorisationHandlerFactory = authorisationHandlerFactory;
        _modelMappings = modelMappings;
        _assistantMappings = assistantMappings;
        _enforceMappedModels = enforceMappedModels;
        _maxConcurrency = maxConcurrency;
        _autoPopulateEmptyUserId = autoPopulateEmptyUserId;
        _logMissingModelMappingsAsInformation = logMissingModelMappingsAsInformation;
    }

    public void RegisterServices(HttpMessageHandler? httpMessageHandler, IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .ConfigureHttpClient(x => x.Timeout = OpenAILikeDownstreamEndpointAdapter.MaxTimeToWaitForOpenAIResponse)
            .AddPolicyHandler(ResiliencyStrategy.Build(_maxConcurrency))
            .ConfigurePrimaryHttpMessageHandler(() => httpMessageHandler ?? new HttpClientHandler());
    }

    public static string ConfigName => "AzureOpenAIEndpoint";

    public static IDownstreamEndpointAdapterFactory BuildFromConfig(
        ILogger logger,
        TypeAndNameConfig config,
        IDictionary<string, IEndpointAuthorisationHandlerFactory> authorisationHandlerFactories)
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

        //Run a diagnostics check. Runs synchronously but will really help people who have issues getting connectivity, moving quicker. 
        var diagnostics = new AzureOpenAIDownstreamEndpointDiagnostics(
            logger,
            config.Name!,
            new Uri(Guard.NotNull(properties.LanguageEndpoint, nameof(properties.LanguageEndpoint)))
        );
        diagnostics.RunDiagnostics().Wait();
        
        var authHandler = Guard.NotNullOrEmptyOrWhitespace(authenticationType, nameof(config.Type)).ToLowerInvariant() switch
        {
            "apikey" => new KeyAuthFactory(properties.ApiKey!),
            "entra" => new EntraAuthFactory(),
            "entrapassthrough" => new BearerTokenPassThroughAuthFactory(),
            _ => authorisationHandlerFactories.TryGetValue(authenticationType!, out var factory) ? factory : throw new ArgumentException("Missing Backend Authenticator named {Name}", authenticationType)
        };

        return new AzureOpenAIDownstreamEndpointAdapterFactory(
            config.Name!,
            Guard.NotNull(properties.LanguageEndpoint, nameof(properties.LanguageEndpoint)),
            authHandler,
            properties.ModelMappings ?? new Dictionary<string, string>(),
            properties.AssistantMappings ?? new Dictionary<string, string>(),
            properties.EnforceMappedModels ?? false,
            properties.MaxConcurrency,
            properties.AutoPopulateEmptyUserId ?? false,
            properties.LogMissingModelMappingsAsInformation ?? false);
    }

    public IDownstreamEndpointAdapter Build()
    {
        return new AzureOpenAIDownstreamEndpointAdapter(
            _id,
            _languageUrl,
            _endpointName,
            _modelMappings,
            _assistantMappings,
            _authorisationHandlerFactory.Build(),
            _enforceMappedModels,
            _autoPopulateEmptyUserId,
            _logMissingModelMappingsAsInformation);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "AzureOpenAI",
            Url = _languageUrl,
            Mappings = _modelMappings,
            AssistantMappings = _assistantMappings,
            Auth = _authorisationHandlerFactory.WriteDebug(),
            AutoPopulateEmptyUserId = _autoPopulateEmptyUserId
        };
    }
}