using AICentral.Core;

namespace AICentral.Steps.Auth.Entra;

public class EntraClientAuthProvider : IAICentralClientAuthStep
{
    public Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.Next(context, aiCallInformation, cancellationToken);
    }

}