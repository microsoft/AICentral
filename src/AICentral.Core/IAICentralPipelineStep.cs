using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

public interface IAICentralPipelineStep
{
    Task<AICentralResponse> Handle(HttpContext context, IncomingCallDetails aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);

    Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse, Dictionary<string,StringValues> rawHeaders);
}
