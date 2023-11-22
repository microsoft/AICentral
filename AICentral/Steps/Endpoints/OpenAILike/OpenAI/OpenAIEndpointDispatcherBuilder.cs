using AICentral.Configuration.JSON;
using AICentral.Core;

namespace AICentral.Steps.Endpoints.OpenAILike.OpenAI;

public class OpenAIEndpointDispatcherBuilder : IAICentralEndpointDispatcherBuilder
{
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string _apiKey;
    private readonly string? _organization;
    private readonly int? _maxConcurrency;
    private readonly string _id;

    public OpenAIEndpointDispatcherBuilder(Dictionary<string, string> modelMappings,
        string apiKey,
        string? organization, 
        int? maxConcurrency)
    {
        _id = Guid.NewGuid().ToString();
        _modelMappings = modelMappings;
        _apiKey = apiKey;
        _organization = organization;
        _maxConcurrency = maxConcurrency;
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .AddPolicyHandler(ResiliencyStrategy.Build(_maxConcurrency));
    }

    public static string ConfigName => "OpenAIEndpoint";

    public static IAICentralEndpointDispatcherBuilder BuildFromConfig(ILogger logger, IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.AICentralPipelineOpenAIEndpointPropertiesConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");
        
        return new OpenAIEndpointDispatcherBuilder(
            Guard.NotNull(properties!.ModelMappings, configurationSection, nameof(properties.ModelMappings)),
            Guard.NotNull(properties.ApiKey, configurationSection, nameof(properties.ApiKey)),
            properties.Organization,
            properties.MaxConcurrency
            );
    }

    public IAICentralEndpointDispatcher Build()
    {
        return new OpenAIEndpointDispatcher(_id, _modelMappings, _apiKey, _organization);
    }
}