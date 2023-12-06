namespace AICentral.Core;

public interface IAICentralPipelineStep
{
    Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);
}
