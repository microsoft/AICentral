namespace AICentral.Pipelines.Endpoints.EndpointAuth;

public interface IEndpointAuthorisationHandler
{
    Task ApplyAuthorisationToRequest(HttpRequest incomingRequest, HttpRequestMessage outgoingRequest);
    object WriteDebug();
}