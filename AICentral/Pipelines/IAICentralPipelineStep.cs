namespace AICentral.Pipelines;

public interface IAICentralPipelineStep
{
    static virtual string ConfigName  => throw new NotImplementedException();

    static virtual IAICentralPipelineStep BuildFromConfig(
        Dictionary<string, string> parameters) => throw new NotImplementedException();

    Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);

    object WriteDebug();

}