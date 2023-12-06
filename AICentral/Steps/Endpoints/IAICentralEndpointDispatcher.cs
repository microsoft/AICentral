using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.Endpoints;

public interface IAICentralEndpointDispatcher
{
    Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation callInformation,
        Func<AICentralRequestInformation, HttpResponseMessage, Dictionary<string, StringValues>,
            Task<AICentralResponse>> responseHandler,
        CancellationToken cancellationToken);
}