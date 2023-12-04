using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.Endpoints;

public interface IAICentralEndpointDispatcher
{
    Task<(AICentralRequestInformation, HttpResponseMessage)> Handle(
        HttpContext context, 
        AICallInformation callInformation,
        CancellationToken cancellationToken);

    Dictionary<string, StringValues> SanitiseHeaders(HttpContext context, HttpResponseMessage openAiResponse);
}