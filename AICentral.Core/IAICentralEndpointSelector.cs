namespace AICentral.Core;

public interface IAICentralEndpointSelector
{
    Task<AICentralResponse> Handle(
        HttpContext context, 
        AICallInformation aiCallInformation, 
        bool isLastChance,
        CancellationToken cancellationToken);

    IEnumerable<IAICentralEndpointDispatcher> ContainedEndpoints();
}