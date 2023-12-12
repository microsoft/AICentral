using AICentral.Core;

namespace AICentral.Auth.AllowAnonymous;

public class AllowAnonymousClientAuthProvider : IAICentralClientAuthStep
{
    public Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        return pipeline.Next(context, aiCallInformation, cancellationToken);
    }

    public static readonly AllowAnonymousClientAuthProvider Instance = new AllowAnonymousClientAuthProvider();
}