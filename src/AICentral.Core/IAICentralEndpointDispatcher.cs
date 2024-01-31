namespace AICentral.Core;

public interface IAICentralEndpointDispatcher
{
    /// <summary>
    /// Either handles a request to an endpoint, or dispatches a request to another Endpoint Selector referenced by an Endpoint Selector (to allow scenarios such as prioritised fail-over to an existing random cluster of endpoints) 
    /// </summary>
    Task<AICentralResponse> Handle(
        HttpContext context,
        IncomingCallDetails callInformation,
        bool isLastChance,
        IAICentralResponseGenerator responseGenerator,
        CancellationToken cancellationToken);

    bool IsAffinityRequestToMe(string affinityHeaderValue);

}