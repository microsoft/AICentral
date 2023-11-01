namespace AICentral.PipelineComponents.Endpoints;

public interface IEndpointAuthorisationHandler
{
    Task ApplyAuthorisationToRequest(HttpRequest incomingRequest, HttpRequestMessage outgoingRequest);
    object WriteDebug();
}