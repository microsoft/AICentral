using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

public interface IAICentralResponseGenerator
{
    Task<AICentralResponse> BuildResponse(DownstreamRequestInformation downstreamRequestInformation,
        HttpContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> sanitisedResponseHeaders,
        CancellationToken cancellationToken);
}