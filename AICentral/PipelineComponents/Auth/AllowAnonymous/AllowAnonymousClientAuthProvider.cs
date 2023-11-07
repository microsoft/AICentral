using AICentral.PipelineComponents.Endpoints;

namespace AICentral.PipelineComponents.Auth.AllowAnonymous;

public class AllowAnonymousClientAuthProvider : IAICentralClientAuthStep
{
    public Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        AICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        return pipeline.Next(context, aiCallInformation, cancellationToken);
    }


    public object WriteDebug()
    {
        return new { auth = "No Consumer Auth" };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }
}