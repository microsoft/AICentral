namespace AICentral.Core;

public delegate Task<AICentralResponse> AIHandler(
    IRequestContext context, 
    string? deploymentName, 
    string? assistantName,
    AICallType callType, 
    CancellationToken cancellationToken);