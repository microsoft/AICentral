using AICentral.PipelineComponents.Endpoints;

namespace AICentral.PipelineComponents.EndpointSelectors.Single;

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

    public static IAICentralEndpointSelectorBuilder BuildFromConfig(Dictionary<string, string> parameters, Dictionary<string, IAICentralEndpointDispatcherBuilder> endpoints)
    {
        return new SingleEndpointSelectorBuilder(endpoints[parameters["Endpoint"]]);
    }
}