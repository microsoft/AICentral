using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors;

internal class EndpointSelectorAdapter : IAICentralEndpointDispatcher
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
}