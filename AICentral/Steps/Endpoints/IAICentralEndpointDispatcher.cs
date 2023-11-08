using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.Endpoints;

public interface IAICentralEndpointDispatcher
{
    Task<(AICentralRequestInformation, HttpResponseMessage)> Handle(
        HttpContext context, 
        AICallInformation callInformation,
        CancellationToken cancellationToken);

    object WriteDebug();
    Dictionary<string, StringValues> SanitiseHeaders(HttpResponseMessage openAiResponse);
}