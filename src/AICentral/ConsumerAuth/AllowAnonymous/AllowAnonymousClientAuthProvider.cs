using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.ConsumerAuth.AllowAnonymous;

public class AllowAnonymousClientAuthProvider : IPipelineStep
{
    public Task<AICentralResponse> Handle(IRequestContext context, IncomingCallDetails aiCallInformation,
        NextPipelineStep next, CancellationToken cancellationToken)
    {
        return next(context, aiCallInformation, cancellationToken);
    }

    public static readonly AllowAnonymousClientAuthProvider Instance = new();
    
    
    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }

}