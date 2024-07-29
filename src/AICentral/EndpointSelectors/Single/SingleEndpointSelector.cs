using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.EndpointSelectors.Single;

public class SingleEndpointSelector : IEndpointSelector
{
    private readonly IEndpointDispatcher _endpoint;

    public SingleEndpointSelector(IEndpointDispatcher endpoint)
    {
        _endpoint = endpoint;
    }

    public Task<AICentralResponse> Handle(
        IRequestContext context,
        IncomingCallDetails aiCallInformation,
        bool isLastChance,
        IResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        return _endpoint.Handle(
            context,
            aiCallInformation,
            isLastChance,
            responseGenerator,
            cancellationToken);
    }

    public IEnumerable<IEndpointDispatcher> ContainedEndpoints()
    {
        return new[] { _endpoint };
    }
    
    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}