namespace AICentral.Core;

public interface IPipelineExecutor
{
    Task<AICentralResponse> Next(HttpContext context, IncomingCallDetails requestDetails,
        CancellationToken cancellationToken);
}