using AICentral.Core;

namespace AICentral.Steps.EndpointSelectors;

public class EndpointSelectorAdapter : IAICentralEndpointDispatcher
{
    private readonly IAICentralEndpointSelectorFactory _endpointSelectorFactory;

    public EndpointSelectorAdapter(IAICentralEndpointSelectorFactory endpointSelectorFactory)
    {
        _endpointSelectorFactory = endpointSelectorFactory;
    }

    /// <summary>
    /// Don't worry about the response handler
    /// </summary>
    /// <param name="context"></param>
    /// <param name="callInformation"></param>
    /// <param name="isLastChance"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation callInformation,
        bool isLastChance,
        CancellationToken cancellationToken)
    {
        return _endpointSelectorFactory.Build().Handle(context, callInformation, isLastChance, cancellationToken);
    }

    public bool IsAffinityRequestToMe(string affinityHeaderValue)
    {
        return false;
    }

    public IEnumerable<IAICentralEndpointDispatcher> ContainedEndpoints()
    {
        foreach (var endpoint in _endpointSelectorFactory.Build().ContainedEndpoints())
        {
            if (endpoint is EndpointSelectorAdapter endpointSelectorAdapter)
            {
                foreach (var wrappedEndpoint in endpointSelectorAdapter.ContainedEndpoints())
                {
                    yield return wrappedEndpoint;
                }
            }
            else
            {
                yield return endpoint;
            }
        }
    }
}