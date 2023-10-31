using AICentral.PipelineComponents.Endpoints;

namespace AICentral.PipelineComponents.EndpointSelectors.Single;

public class SingleEndpointSelectorBuilder: IAICentralEndpointSelectorBuilder
{
    private readonly IAiCentralEndpointDispatcherBuilder _endpointDispatcherBuilder;

    public SingleEndpointSelectorBuilder(IAiCentralEndpointDispatcherBuilder endpointDispatcherBuilder)
    {
        _endpointDispatcherBuilder = endpointDispatcherBuilder;
    }

    public IAICentralEndpointSelector Build(Dictionary<IAiCentralEndpointDispatcherBuilder, IAICentralEndpointDispatcher> buildEndpoints)
    {
        return new SingleEndpointSelector(buildEndpoints[_endpointDispatcherBuilder]);
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "SingleEndpoint";

    public static IAICentralEndpointSelectorBuilder BuildFromConfig(Dictionary<string, string> parameters, Dictionary<string, IAiCentralEndpointDispatcherBuilder> endpoints)
    {
        return new SingleEndpointSelectorBuilder(endpoints[parameters["Endpoint"]]);
    }
}