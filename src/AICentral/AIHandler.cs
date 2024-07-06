using AICentral.Core;

namespace AICentral;

public delegate Task<AICentralResponse> AIHandler(HttpContext context, string? deploymentName, string? assistantName,
    AICallType callType, CancellationToken cancellationToken);