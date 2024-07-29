namespace AICentral.Core;

public delegate Task<AICentralResponse> NextPipelineStep(IRequestContext context, IncomingCallDetails requestDetails, CancellationToken cancellationToken);
