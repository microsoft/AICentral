using AICentral.Core;
using AICentral.Steps.Endpoints;
using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.EndpointSelectors.Single;

internal class EndpointSelectorAdapter : IAICentralEndpointDispatcher
{
    private readonly IEndpointSelector _endpointSelector;

    public EndpointSelectorAdapter(IEndpointSelector endpointSelector)
    {
        _endpointSelector = endpointSelector;
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
        return _endpointSelector.Handle(context, callInformation, isLastChance, cancellationToken);
    }
}