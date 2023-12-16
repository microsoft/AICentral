namespace AICentral.Core;

public interface IAICentralPipelineExecutor
{
    Task<AICentralResponse> Next(HttpContext context, IncomingCallDetails requestDetails,
        CancellationToken cancellationToken);
}