using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors.Single;

public class SingleEndpointSelector : IEndpointSelector
{
    private readonly IAICentralEndpointDispatcher _endpoint;

    public SingleEndpointSelector(IAICentralEndpointDispatcher endpoint)
    {
        _endpoint = endpoint;
    }

    public async Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation aiCallInformation,
        bool isLastChance,
        CancellationToken cancellationToken)
    {
        return await _endpoint.Handle(
            context,
            aiCallInformation,
            isLastChance,
            cancellationToken);
    }
}