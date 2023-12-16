using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.EndpointSelectors.Single;

public class SingleEndpointSelector : IAICentralEndpointSelector
{
    private readonly IAICentralEndpointDispatcher _endpoint;

    public SingleEndpointSelector(IAICentralEndpointDispatcher endpoint)
    {
        _endpoint = endpoint;
    }

    public Task<AICentralResponse> Handle(
        HttpContext context,
        IncomingCallDetails aiCallInformation,
        bool isLastChance,
        IAICentralResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        return _endpoint.Handle(
            context,
            aiCallInformation,
            isLastChance,
            responseGenerator,
            cancellationToken);
    }

    public IEnumerable<IAICentralEndpointDispatcher> ContainedEndpoints()
    {
        return new[] { _endpoint };
    }
    
    public Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}