using AICentral.Configuration.JSON;
using AICentral.Core;

namespace AICentral.Steps.Endpoints.OpenAILike.AzureOpenAI;

public class AzureOpenAIEndpointDispatcherBuilder : IAICentralEndpointDispatcherBuilder
{
    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly string _languageUrl;
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _id;
    private readonly int? _maxConcurrency;

    public AzureOpenAIEndpointDispatcherBuilder(
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
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .AddPolicyHandler(ResiliencyStrategy.Build(_maxConcurrency));
    }

    public static string ConfigName => "AzureOpenAIEndpoint";

    public static IAICentralEndpointDispatcherBuilder BuildFromConfig(ILogger logger, IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties")
            .Get<ConfigurationTypes.AICentralPipelineAzureOpenAIEndpointPropertiesConfig>();
        
        Guard.NotNull(properties, configurationSection, "Properties");

        var modelMappings = properties!.ModelMappings;
        var authenticationType = properties.AuthenticationType;
        if (modelMappings == null)
        {
            logger.LogWarning("Pipeline {ConfigurationSectionPath} has no model mappings configured. All requests will use default behaviour of passing model name straight through", configurationSection.Path);
            modelMappings = new Dictionary<string, string>();
        }
        if (authenticationType == null)
        {
            logger.LogWarning("Pipeline {ConfigurationSectionPath} has no AuthType configured. Defaulting to AAD pass-through", configurationSection.Path);
            authenticationType = AuthenticationType.EntraPassThrough;
        }

        return new AzureOpenAIEndpointDispatcherBuilder(
            Guard.NotNull(properties!.LanguageEndpoint, configurationSection, nameof(properties.LanguageEndpoint)),
            modelMappings,
            authenticationType.Value,
            properties.ApiKey,
            properties.MaxConcurrency);
    }

    public IAICentralEndpointDispatcher Build()
    {
        return new AzureOpenAIEndpointDispatcher(_id, _languageUrl, _modelMappings, _authHandler);
    }
}