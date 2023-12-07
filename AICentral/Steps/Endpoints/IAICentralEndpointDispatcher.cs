using AICentral.Core;

namespace AICentral.Steps.Endpoints;

public interface IAICentralEndpointDispatcher
{
    Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation callInformation,
        bool isLastChance,
        CancellationToken cancellationToken);
}