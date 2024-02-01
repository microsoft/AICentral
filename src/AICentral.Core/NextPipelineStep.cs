namespace AICentral.Core;

public delegate Task<AICentralResponse> NextPipelineStep(HttpContext context, IncomingCallDetails requestDetails, CancellationToken cancellationToken);
