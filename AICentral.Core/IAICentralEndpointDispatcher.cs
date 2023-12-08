namespace AICentral.Core;

public interface IAICentralEndpointDispatcher
{
    Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation callInformation,
        bool isLastChance,
        CancellationToken cancellationToken);

    bool IsAffinityRequestToMe(string affinityHeaderValue);
    
}