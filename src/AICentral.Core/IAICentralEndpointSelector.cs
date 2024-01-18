using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

public interface IAICentralEndpointSelector
{
    Task<AICentralResponse> Handle(
        HttpContext context,
        IncomingCallDetails aiCallInformation,
        bool isLastChance,
        IAICentralResponseGenerator responseGenerator,
        CancellationToken cancellationToken);

    IEnumerable<IAICentralEndpointDispatcher> ContainedEndpoints();

    Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders);
}