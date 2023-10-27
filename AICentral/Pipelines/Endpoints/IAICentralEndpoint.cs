namespace AICentral.Pipelines.Endpoints;

public interface IAICentralEndpoint
{
    static virtual string ConfigName => throw new NotImplementedException();

    static virtual IAICentralEndpoint BuildFromConfig(Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }

    Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken);

    object WriteDebug();
}