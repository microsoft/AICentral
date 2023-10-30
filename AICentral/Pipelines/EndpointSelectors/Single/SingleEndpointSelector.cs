using AICentral.Pipelines.Endpoints;

namespace AICentral.Pipelines.EndpointSelectors.Single;

public class SingleEndpointSelector: IAICentralEndpointSelector, IAICentralEndpointSelectorRuntime
{
    private readonly IAICentralEndpointRuntime _endpoint;

    public SingleEndpointSelector(IAICentralEndpointRuntime endpoint)
    {
        _endpoint = endpoint;
    }
    public async Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        return await _endpoint.Handle(context, pipeline, cancellationToken);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "SingleEndpoint",
            Endpoint = _endpoint.WriteDebug()
        };
    }

    public IAICentralEndpointSelectorRuntime Build()
    {
        return this;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

    public static string ConfigName => "SingleEndpoint";

    public static IAICentralEndpointSelector BuildFromConfig(Dictionary<string, string> parameters, Dictionary<string, IAICentralEndpointRuntime> endpoints)
    {
        return new SingleEndpointSelector(endpoints[parameters["Endpoint"]]);
    }
}
