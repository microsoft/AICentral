namespace AICentral.Pipelines;

public interface IAICentralPipelineStep<T> where T: IAICentralPipelineStepRuntime
{
    static virtual string ConfigName  => throw new NotImplementedException();

    static virtual IAICentralPipelineStep<T> BuildFromConfig(
        Dictionary<string, string> parameters) => throw new NotImplementedException();

    T Build();

    void RegisterServices(IServiceCollection services);

    void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route);

}


public interface IAICentralPipelineStepRuntime
{
    Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);

    object WriteDebug();

}