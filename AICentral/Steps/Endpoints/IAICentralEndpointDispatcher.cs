using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.Endpoints;

public interface IAICentralEndpointDispatcher
{
    Task<(AICentralRequestInformation RequestInformation, HttpResponseMessage RawResponseMessage,
        Dictionary<string, StringValues> SanistisedHeaders)> Handle(
        HttpContext context,
        AICallInformation callInformation,
        CancellationToken cancellationToken);
}