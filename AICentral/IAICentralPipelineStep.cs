namespace AICentral;

public interface IAICentralPipelineStep
{
    Task<AICentralResponse> Handle(
        HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);

    object WriteDebug();

    void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route);
    
}
