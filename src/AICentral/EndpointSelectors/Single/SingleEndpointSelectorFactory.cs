using AICentral.Core;

namespace AICentral.EndpointSelectors.Single;

public class SingleEndpointSelectorFactory : IEndpointSelectorFactory
{
    private readonly IEndpointDispatcherFactory _endpointRequestResponseHandlerFactory;
    private readonly Lazy<SingleEndpointSelector> _endpointSelector;

    public SingleEndpointSelectorFactory(IEndpointDispatcherFactory endpointRequestResponseHandlerFactory)
    {
        _endpointRequestResponseHandlerFactory = endpointRequestResponseHandlerFactory;
        _endpointSelector =
            new Lazy<SingleEndpointSelector>(() => new SingleEndpointSelector(endpointRequestResponseHandlerFactory.Build()));
    }

    public IEndpointSelector Build()
    {
        return _endpointSelector.Value;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "SingleEndpoint";

    public static IEndpointSelectorFactory BuildFromConfig(
        ILogger logger,
        TypeAndNameConfig config,
        Dictionary<string, IEndpointDispatcherFactory> endpoints)
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
            Endpoints = new[] { _endpointRequestResponseHandlerFactory.WriteDebug() }
        };
    }
}