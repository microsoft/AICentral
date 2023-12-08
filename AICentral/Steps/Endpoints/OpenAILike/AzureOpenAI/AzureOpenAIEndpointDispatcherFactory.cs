using AICentral.Configuration.JSON;
using AICentral.Core;

namespace AICentral.Steps.Endpoints.OpenAILike.AzureOpenAI;

public class AzureOpenAIEndpointDispatcherFactory : IAICentralEndpointDispatcherFactory
{
    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly string _languageUrl;
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _id;
    private readonly int? _maxConcurrency;
    private readonly Lazy<IAICentralEndpointDispatcher> _endpointDispatcher;

    public AzureOpenAIEndpointDispatcherFactory(
        string endpointName,
        string languageUrl,
        Dictionary<string, string> modelMappings,
        AuthenticationType authenticationType,
        string? authenticationKey,
        int? maxConcurrency = null)
    {
        _id = Guid.NewGuid().ToString();

        _languageUrl = languageUrl;
        _modelMappings = modelMappings;
        _maxConcurrency = maxConcurrency;

        _authHandler = authenticationType switch
        {
            AuthenticationType.ApiKey => new KeyAuth(authenticationKey ??
                                                     throw new ArgumentException(
                                                         "Missing api-key for Authentication Type")),
            AuthenticationType.Entra => new EntraAuth(),
            AuthenticationType.EntraPassThrough => new BearerTokenPassThroughAuth(),
            _ => throw new ArgumentOutOfRangeException(nameof(authenticationType), authenticationType, null)
        };

        _endpointDispatcher = new Lazy<IAICentralEndpointDispatcher>(() =>
            new AzureOpenAIEndpointDispatcher(_id, _languageUrl, endpointName, _modelMappings, _authHandler));
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .AddPolicyHandler(ResiliencyStrategy.Build(_maxConcurrency));
    }

    public static string ConfigName => "AzureOpenAIEndpoint";

    public static IAICentralEndpointDispatcherFactory BuildFromConfig(ILogger logger,
        IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties")
            .Get<ConfigurationTypes.AICentralPipelineAzureOpenAIEndpointPropertiesConfig>();

        Guard.NotNull(properties, configurationSection, "Properties");

        var modelMappings = properties!.ModelMappings;
        var authenticationType = properties.AuthenticationType;
        if (modelMappings == null)
        {
            logger.LogWarning(
                "Pipeline {ConfigurationSectionPath} has no model mappings configured. All requests will use default behaviour of passing model name straight through",
                configurationSection.Path);
            modelMappings = new Dictionary<string, string>();
        }

        if (authenticationType == null)
        {
            logger.LogWarning(
                "Pipeline {ConfigurationSectionPath} has no AuthType configured. Defaulting to AAD pass-through",
                configurationSection.Path);
            authenticationType = AuthenticationType.EntraPassThrough;
        }

        return new AzureOpenAIEndpointDispatcherFactory(
            configurationSection.GetValue<string>("Name")!,
            Guard.NotNull(properties!.LanguageEndpoint, configurationSection, nameof(properties.LanguageEndpoint)),
            modelMappings,
            authenticationType.Value,
            properties.ApiKey,
            properties.MaxConcurrency);
    }

    public IAICentralEndpointDispatcher Build()
    {
        return _endpointDispatcher.Value;
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