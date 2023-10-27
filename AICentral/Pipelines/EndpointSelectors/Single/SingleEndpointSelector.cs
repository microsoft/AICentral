using AICentral.Pipelines.Endpoints;

namespace AICentral.Pipelines.EndpointSelectors.Single;

public class SingleEndpointSelector: IAICentralEndpointSelector
{
    private readonly IAICentralEndpoint _endpoint;

    public SingleEndpointSelector(IAICentralEndpoint endpoint)
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

    public static string ConfigName => "SingleEndpoint";

    public static IAICentralEndpointSelector BuildFromConfig(Dictionary<string, string> parameters, Dictionary<string, IAICentralEndpoint> endpoints)
    {
        return new SingleEndpointSelector(endpoints[parameters["Endpoint"]]);
    }
}
