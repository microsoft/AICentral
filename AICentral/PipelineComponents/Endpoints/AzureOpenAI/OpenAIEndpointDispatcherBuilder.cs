using AICentral.Configuration.JSON;

namespace AICentral.PipelineComponents.Endpoints.AzureOpenAI;

public class OpenAIEndpointDispatcherBuilder : IAICentralEndpointDispatcherBuilder
{
    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly string _languageUrl;
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _id;

    public OpenAIEndpointDispatcherBuilder(
        string languageUrl,
        Dictionary<string, string> modelMappings,
        AuthenticationType authenticationType,
        string? authenticationKey)
    {
        _id = Guid.NewGuid().ToString();

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
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .AddPolicyHandler(ResiliencyStrategy.Build());
    }

    public static string ConfigName => "AzureOpenAIEndpoint";

    public static IAICentralEndpointDispatcherBuilder BuildFromConfig(IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.AICentralPipelineAzureOpenAIEndpointPropertiesConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");
        
        return new OpenAIEndpointDispatcherBuilder(
            Guard.NotNull(properties!.LanguageEndpoint, configurationSection, nameof(properties.LanguageEndpoint)),
            Guard.NotNull(properties.ModelMappings, configurationSection, nameof(properties.ModelMappings)),
            Guard.NotNull(properties.AuthenticationType, configurationSection, nameof(properties.AuthenticationType)),
            properties.ApiKey);
    }

    public IAICentralEndpointDispatcher Build()
    {
        return new AzureOpenAIEndpointDispatcher(_id, _languageUrl, _modelMappings, _authHandler);
    }
}