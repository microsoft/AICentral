using AICentral.Core;

namespace AICentral.EndpointSelectors.Single;

public class SingleEndpointSelectorFactory : IAICentralEndpointSelectorFactory
{
    private readonly IEndpointRequestResponseHandlerFactory _endpointRequestResponseHandlerFactory;
    private readonly Lazy<SingleEndpointSelector> _endpointSelector;

    public SingleEndpointSelectorFactory(IEndpointRequestResponseHandlerFactory endpointRequestResponseHandlerFactory)
    {
        _endpointRequestResponseHandlerFactory = endpointRequestResponseHandlerFactory;
        _endpointSelector =
            new Lazy<SingleEndpointSelector>(() => new SingleEndpointSelector(endpointRequestResponseHandlerFactory.Build()));
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
        Dictionary<string, IEndpointRequestResponseHandlerFactory> endpoints)
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