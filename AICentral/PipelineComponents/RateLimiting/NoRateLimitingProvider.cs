namespace AICentral.PipelineComponents.RateLimiting;

public class NoRateLimitingProvider : IAICentralPipelineStepBuilder<IAICentralPipelineStep>, IAICentralPipelineStep
{
    public void RegisterServices(IServiceCollection services)
    {
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        //no-op
    }

    public static IAICentralPipelineStepBuilder<IAICentralPipelineStep> BuildFromConfig(Dictionary<string, string> parameters)
    {
        return new NoRateLimitingProvider();
    }

    public IAICentralPipelineStep Build()
    {
        return this;
    }

    public Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        return pipeline.Next(context, cancellationToken);
    }

    public object WriteDebug()
    {
        return new { };
    }

    public static string ConfigName => "NoOp";
}