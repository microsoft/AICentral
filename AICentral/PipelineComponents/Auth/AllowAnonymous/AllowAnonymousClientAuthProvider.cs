namespace AICentral.PipelineComponents.Auth.AllowAnonymous;

public class AllowAnonymousClientAuthProvider : IAICentralClientAuthStep
{
    public Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        return pipeline.Next(context, cancellationToken);
    }


    public object WriteDebug()
    {
        return new { auth = "No Consumer Auth" };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }
}