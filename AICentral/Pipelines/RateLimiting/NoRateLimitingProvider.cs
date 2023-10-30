namespace AICentral.Pipelines.RateLimiting;

public class NoRateLimitingProvider : IAICentralPipelineStep<IAICentralPipelineStepRuntime>, IAICentralPipelineStepRuntime
{
    public void RegisterServices(IServiceCollection services)
    {
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        //no-op
    }

    public static IAICentralPipelineStep<IAICentralPipelineStepRuntime> BuildFromConfig(IConfigurationSection configurationSection,
        Dictionary<string, string> parameters)
    {
        return new NoRateLimitingProvider();
    }

    public IAICentralPipelineStepRuntime Build()
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