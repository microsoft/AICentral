using AICentral.Configuration.JSON;

namespace AICentral.Steps.Endpoints.OpenAILike.OpenAI;

public class OpenAIEndpointDispatcherBuilder : IAICentralEndpointDispatcherBuilder
{
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _apiKey;
    private readonly string? _organization;
    private readonly string _id;

    public OpenAIEndpointDispatcherBuilder(
        Dictionary<string, string> modelMappings, 
        string apiKey,
        string? organization)
    {
        _id = Guid.NewGuid().ToString();
        _modelMappings = modelMappings;
        _apiKey = apiKey;
        _organization = organization;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .AddPolicyHandler(ResiliencyStrategy.Build());
    }

    public static string ConfigName => "OpenAIEndpoint";

    public static IAICentralEndpointDispatcherBuilder BuildFromConfig(IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.AICentralPipelineOpenAIEndpointPropertiesConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");
        
        return new OpenAIEndpointDispatcherBuilder(
            Guard.NotNull(properties!.ModelMappings, configurationSection, nameof(properties.ModelMappings)),
            Guard.NotNull(properties.ApiKey, configurationSection, nameof(properties.ApiKey)),
            properties.Organization
            );
    }

    public IAICentralEndpointDispatcher Build()
    {
        return new OpenAIEndpointDispatcher(_id, _modelMappings, _apiKey, _organization);
    }
}