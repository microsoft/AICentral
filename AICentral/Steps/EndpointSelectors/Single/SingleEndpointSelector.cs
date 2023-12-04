using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors.Single;

public class SingleEndpointSelector : EndpointSelectorBase
{
    private readonly IAICentralEndpointDispatcher _endpoint;

    public SingleEndpointSelector(IAICentralEndpointDispatcher endpoint)
    {
        _endpoint = endpoint;
    }

    public override async Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        CancellationToken cancellationToken)
    {
        var responseMessage = await _endpoint.Handle(context, aiCallInformation, cancellationToken);
        return await HandleResponse(
            context.RequestServices.GetRequiredService<ILogger<SingleEndpointSelector>>(),
            context,
            _endpoint,
            responseMessage.Item1,
            responseMessage.Item2,
            true,
            cancellationToken
        );
    }

}