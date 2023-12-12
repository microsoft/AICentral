namespace AICentral.Core;

public interface IAICentralPipelineExecutor
{
    Task<AICentralResponse> Next(HttpContext context, AICallInformation requestDetails,
        CancellationToken cancellationToken);
}