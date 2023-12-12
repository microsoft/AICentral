using AICentral.Core;

namespace AICentral.EndpointSelectors.Single;

public class SingleEndpointSelectorFactory : IAICentralEndpointSelectorFactory
{
    private readonly IAICentralEndpointDispatcherFactory _endpointDispatcherFactory;
    private readonly Lazy<SingleIaiCentralEndpointSelector> _endpointSelector;

    public SingleEndpointSelectorFactory(IAICentralEndpointDispatcherFactory endpointDispatcherFactory)
    {
        _endpointDispatcherFactory = endpointDispatcherFactory;
        _endpointSelector =
            new Lazy<SingleIaiCentralEndpointSelector>(() => new SingleIaiCentralEndpointSelector(endpointDispatcherFactory.Build()));
    }

    public IAICentralEndpointSelector Build()
    {
        return _endpointSelector.Value;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "SingleEndpoint";

    public static IAICentralEndpointSelectorFactory BuildFromConfig(
        ILogger logger,
        AICentralTypeAndNameConfig config,
        Dictionary<string, IAICentralEndpointDispatcherFactory> endpoints)
    {
        var properties = config.TypedProperties<SingleEndpointConfig>();

        var endpoint = properties.Endpoint;
        endpoint = Guard.NotNull(endpoint, "Endpoint");
        return new SingleEndpointSelectorFactory(endpoints[endpoint]);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "SingleEndpoint",
            Endpoints = new[] { _endpointDispatcherFactory.WriteDebug() }
        };
    }
}

public class SingleEndpointConfig
{
    public string? Endpoint { get; init; }
}