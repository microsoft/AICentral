using AICentral.PipelineComponents.Endpoints;

namespace AICentral.PipelineComponents.EndpointSelectors.Single;

public class SingleEndpointSelector: IAICentralEndpointSelector
{
    private readonly IAICentralEndpointDispatcher _endpoint;

    public SingleEndpointSelector(IAICentralEndpointDispatcher endpoint)
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

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }
    
}