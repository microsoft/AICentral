namespace AICentral.Steps.Endpoints;

public interface IEndpointAuthorisationHandler
{
    Task ApplyAuthorisationToRequest(HttpRequest incomingRequest, HttpRequestMessage outgoingRequest);
    object WriteDebug();
}