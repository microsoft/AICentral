using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors.Single;

public class SingleIaiCentralEndpointSelector : IAICentralEndpointSelector
{
    private readonly IAICentralEndpointDispatcher _endpoint;

    public SingleIaiCentralEndpointSelector(IAICentralEndpointDispatcher endpoint)
    {
        _endpoint = endpoint;
    }

    public Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation aiCallInformation,
        bool isLastChance,
        CancellationToken cancellationToken)
    {
        return _endpoint.Handle(
            context,
            aiCallInformation,
            isLastChance,
            cancellationToken);
    }

    public IEnumerable<IAICentralEndpointDispatcher> ContainedEndpoints()
    {
        return new[] { _endpoint };
    }
}