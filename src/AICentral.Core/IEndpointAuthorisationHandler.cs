namespace AICentral.Core;

public interface IEndpointAuthorisationHandler
{
    Task ApplyAuthorisationToRequest(IRequestContext incomingRequest, HttpRequestMessage outgoingRequest);
}