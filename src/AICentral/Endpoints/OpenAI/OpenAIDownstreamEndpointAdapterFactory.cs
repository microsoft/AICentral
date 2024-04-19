using AICentral.Core;

namespace AICentral.Endpoints.OpenAI;

public class OpenAIDownstreamEndpointAdapterFactory : IDownstreamEndpointAdapterFactory
{
    private readonly Dictionary<string, string> _assistantMappings;
    private readonly Dictionary<string, string> _modelMappings;
    private readonly string? _organization;
    private readonly int? _maxConcurrency;
    private readonly Lazy<IDownstreamEndpointAdapter> _endpointDispatcher;
    private readonly string _id;
    private readonly bool _autoPopulateEmptyUserId;

    public OpenAIDownstreamEndpointAdapterFactory(string endpointName,
        Dictionary<string, string> modelMappings,
        Dictionary<string, string> assistantMappings,
        string apiKey,
        string? organization,
        int? maxConcurrency = null, 
        bool autoPopulateEmptyUserId = false)
    {
        _id = Guid.NewGuid().ToString();
        _modelMappings = modelMappings;
        _assistantMappings = assistantMappings;
        _organization = organization;
        _maxConcurrency = maxConcurrency;
        _autoPopulateEmptyUserId = autoPopulateEmptyUserId;

        _endpointDispatcher = new Lazy<IDownstreamEndpointAdapter>(() =>
            new OpenAIDownstreamEndpointAdapter(_id, endpointName, _modelMappings, _assistantMappings, apiKey,
                _organization, _autoPopulateEmptyUserId));
    }

    public void RegisterServices(HttpMessageHandler? httpMessageHandler, IServiceCollection services)
    {
        services.AddHttpClient<HttpAIEndpointDispatcher>(_id)
            .ConfigureHttpClient(x => x.Timeout = OpenAILikeDownstreamEndpointAdapter.MaxTimeToWaitForOpenAIResponse)
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
            properties.AssistantMappings ?? new Dictionary<string, string>(),
            Guard.NotNull(properties.ApiKey, nameof(properties.ApiKey)),
            properties.Organization,
            properties.MaxConcurrency,
            properties.AutoPopulateEmptyUserId ?? false
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
            Url = OpenAIDownstreamEndpointAdapter.OpenAIV1,
            Mappings = _modelMappings,
            AssistantMappings = _assistantMappings,
            Organization = _organization,
            AutoPopulateEmptyUserId = _autoPopulateEmptyUserId
        };
    }
}