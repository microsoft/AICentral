using AICentral.Configuration.JSON;
using AICentral.Core;

namespace AICentral.Steps.Endpoints.OpenAILike.OpenAI;

public class OpenAIEndpointDispatcherFactory : IAICentralEndpointDispatcherFactory
{
    private readonly string _endpointName;
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string? _organization;
    private readonly int? _maxConcurrency;
    private readonly string _id;
    private readonly Lazy<IAICentralEndpointDispatcher> _endpointDispatcher;

    public OpenAIEndpointDispatcherFactory(string endpointName, Dictionary<string, string> modelMappings,
        string apiKey,
        string? organization,
        int? maxConcurrency)
    {
        _id = Guid.NewGuid().ToString();
        _endpointName = endpointName;
        _modelMappings = modelMappings;
        _organization = organization;
        _maxConcurrency = maxConcurrency;

        _endpointDispatcher = new Lazy<IAICentralEndpointDispatcher>(() =>
            new OpenAIEndpointDispatcher(_id, _endpointName, _modelMappings, apiKey, _organization));
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .AddPolicyHandler(ResiliencyStrategy.Build(_maxConcurrency));
    }

    public static string ConfigName => "OpenAIEndpoint";

    public static IAICentralEndpointDispatcherFactory BuildFromConfig(ILogger logger, IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.AICentralPipelineOpenAIEndpointPropertiesConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");
        
        return new OpenAIEndpointDispatcherFactory(
            configurationSection.GetValue<string>("Name")!,
            Guard.NotNull(properties!.ModelMappings, configurationSection, nameof(properties.ModelMappings)),
            Guard.NotNull(properties.ApiKey, configurationSection, nameof(properties.ApiKey)),
            properties.Organization,
            properties.MaxConcurrency
            );
    }

    public IAICentralEndpointDispatcher Build()
    {
        return _endpointDispatcher.Value;
    }
    
    
    public object WriteDebug()
    {
        return new
        {
            Type = "OpenAI",
            Url = OpenAIEndpointDispatcher.OpenAIV1,
            Mappings = _modelMappings,
            Organization = _organization
        };
    }

}