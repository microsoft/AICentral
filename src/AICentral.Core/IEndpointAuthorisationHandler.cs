namespace AICentral.Core;

public interface IEndpointAuthorisationHandler
{
    Task ApplyAuthorisationToRequest(HttpRequest incomingRequest, HttpRequestMessage outgoingRequest);
}