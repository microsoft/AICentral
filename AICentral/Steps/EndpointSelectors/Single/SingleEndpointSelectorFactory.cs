using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors.Single;

public class SingleEndpointSelectorFactory: IAICentralEndpointSelectorFactory
{
    private readonly IAICentralEndpointDispatcherFactory _endpointDispatcherFactory;

    public SingleEndpointSelectorFactory(IAICentralEndpointDispatcherFactory endpointDispatcherFactory)
    {
        _endpointDispatcherFactory = endpointDispatcherFactory;
    }

    public IEndpointSelector Build(Dictionary<IAICentralEndpointDispatcherFactory, IAICentralEndpointDispatcher> buildEndpoints)
    {
        return new SingleEndpointSelector(buildEndpoints[_endpointDispatcherFactory]);
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "SingleEndpoint";

    public static IAICentralEndpointSelectorFactory BuildFromConfig(ILogger logger, IConfigurationSection configSection, Dictionary<string, IAICentralEndpointDispatcherFactory> endpoints)
    {
        var properties = configSection.GetSection("Properties");
        Guard.NotNull(properties, properties, "Properties");

        var endpoint = properties.GetValue<string>("Endpoint");
        endpoint = Guard.NotNull(endpoint, configSection, "Endpoint");
        return new SingleEndpointSelectorFactory(endpoints[endpoint]);
    }
}