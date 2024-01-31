namespace AICentral.Core;

/// <summary>
/// Used to dispatch requests to the correct endpoint, or to an endpoint selector (which will then dispatch onwards).
/// </summary>
/// <remarks>
/// This interface is in the Core library to support Endpoint Selectors that need to dispatch to other Endpoint Selectors.
/// Normal usage does not require you to provide your own implementation of this interface.  
/// </remarks>
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