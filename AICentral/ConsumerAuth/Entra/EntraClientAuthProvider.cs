using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.ConsumerAuth.Entra;

public class EntraClientAuthProvider : IConsumerAuthStep
{
    public Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.Next(context, aiCallInformation, cancellationToken);
    }


    public Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}