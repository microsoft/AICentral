using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.ConsumerAuth.ApiKey;

public class ApiKeyClientAuthProvider : IPipelineStep
{
    public Task<AICentralResponse> Handle(IRequestContext context, IncomingCallDetails aiCallInformation,
        NextPipelineStep next, CancellationToken cancellationToken)
    {
        return next(context, aiCallInformation, cancellationToken);
    }


    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}