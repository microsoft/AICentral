using AICentral.PipelineComponents.Endpoints;

namespace AICentral;

public interface IAICentralPipelineStep
{
    Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);

    object WriteDebug();

    void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route);
    
}
