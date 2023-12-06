using AICentral.Core;

namespace AICentral.Steps.Auth.ApiKey;

public class ApiKeyClientAuthProvider : IAICentralClientAuthStep
{
    public Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        return pipeline.Next(context, aiCallInformation, cancellationToken);
    }
}