using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors.Single;

public class SingleEndpointSelectorBuilder: IAICentralEndpointSelectorBuilder
{
    private readonly IAICentralEndpointDispatcherBuilder _endpointDispatcherBuilder;

    public SingleEndpointSelectorBuilder(IAICentralEndpointDispatcherBuilder endpointDispatcherBuilder)
    {
        _endpointDispatcherBuilder = endpointDispatcherBuilder;
    }

    public IEndpointSelector Build(Dictionary<IAICentralEndpointDispatcherBuilder, IAICentralEndpointDispatcher> buildEndpoints)
    {
        return new SingleEndpointSelector(buildEndpoints[_endpointDispatcherBuilder]);
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "SingleEndpoint";

    public static IAICentralEndpointSelectorBuilder BuildFromConfig(ILogger logger, IConfigurationSection configSection, Dictionary<string, IAICentralEndpointDispatcherBuilder> endpoints)
    {
        var properties = configSection.GetSection("Properties");
        Guard.NotNull(properties, properties, "Properties");

        var endpoint = properties.GetValue<string>("Endpoint");
        endpoint = Guard.NotNull(endpoint, configSection, "Endpoint");
        return new SingleEndpointSelectorBuilder(endpoints[endpoint]);
    }
}