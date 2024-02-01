using AICentral.Core;

namespace AICentral.Endpoints.OpenAI;

public class OpenAIDownstreamEndpointAdapterFactory : IDownstreamEndpointAdapterFactory
{
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string? _organization;
    private readonly int? _maxConcurrency;
    private readonly Lazy<IDownstreamEndpointAdapter> _endpointDispatcher;
    private readonly string _id;

    public OpenAIDownstreamEndpointAdapterFactory(string endpointName, Dictionary<string, string> modelMappings,
        string apiKey,
        string? organization,
        int? maxConcurrency = null)
    {
        _id = Guid.NewGuid().ToString();
        _modelMappings = modelMappings;
        _organization = organization;
        _maxConcurrency = maxConcurrency;

        _endpointDispatcher = new Lazy<IDownstreamEndpointAdapter>(() =>
            new OpenAIIaiCentralDownstreamEndpointAdapter(_id, endpointName, _modelMappings, apiKey, _organization));
    }

    public void RegisterServices(HttpMessageHandler? httpMessageHandler, IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .AddPolicyHandler(ResiliencyStrategy.Build(_maxConcurrency))
            .ConfigurePrimaryHttpMessageHandler(() => httpMessageHandler ?? new HttpClientHandler());
    }

    public static string ConfigName => "OpenAIEndpoint";

    public static IDownstreamEndpointAdapterFactory BuildFromConfig(ILogger logger,
        TypeAndNameConfig config)
    {
        var properties = config.TypedProperties<OpenAIEndpointPropertiesConfig>();
        Guard.NotNull(properties, "Properties");

        return new OpenAIDownstreamEndpointAdapterFactory(
            config.Name!,
            Guard.NotNull(properties.ModelMappings, nameof(properties.ModelMappings)),
            Guard.NotNull(properties.ApiKey, nameof(properties.ApiKey)),
            properties.Organization,
            properties.MaxConcurrency
        );
    }

    public IDownstreamEndpointAdapter Build()
    {
        return _endpointDispatcher.Value;
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "OpenAI",
            Url = OpenAIIaiCentralDownstreamEndpointAdapter.OpenAIV1,
            Mappings = _modelMappings,
            Organization = _organization
        };
    }
}