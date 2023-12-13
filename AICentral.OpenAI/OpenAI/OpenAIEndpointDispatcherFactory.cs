using AICentral.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AICentral.OpenAI.OpenAI;

public class OpenAIEndpointDispatcherFactory : IAICentralEndpointDispatcherFactory
{
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string? _organization;
    private readonly int? _maxConcurrency;
    private readonly Lazy<IAICentralEndpointDispatcher> _endpointDispatcher;
    private readonly string _id;

    public OpenAIEndpointDispatcherFactory(string endpointName, Dictionary<string, string> modelMappings,
        string apiKey,
        string? organization,
        int? maxConcurrency = null)
    {
        _id = Guid.NewGuid().ToString();
        _modelMappings = modelMappings;
        _organization = organization;
        _maxConcurrency = maxConcurrency;

        _endpointDispatcher = new Lazy<IAICentralEndpointDispatcher>(() =>
            new OpenAIEndpointDispatcher(_id, endpointName, _modelMappings, apiKey, _organization));
    }

    public void RegisterServices(HttpMessageHandler? httpMessageHandler, IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .AddPolicyHandler(ResiliencyStrategy.Build(_maxConcurrency))
            .ConfigurePrimaryHttpMessageHandler(() => httpMessageHandler ?? new HttpClientHandler());
    }

    public static string ConfigName => "OpenAIEndpoint";

    public static IAICentralEndpointDispatcherFactory BuildFromConfig(ILogger logger,
        AICentralTypeAndNameConfig config)
    {
        var properties = config.TypedProperties<AICentralPipelineOpenAIEndpointPropertiesConfig>();
        Guard.NotNull(properties, "Properties");

        return new OpenAIEndpointDispatcherFactory(
            config.Name!,
            Guard.NotNull(properties.ModelMappings, nameof(properties.ModelMappings)),
            Guard.NotNull(properties.ApiKey, nameof(properties.ApiKey)),
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