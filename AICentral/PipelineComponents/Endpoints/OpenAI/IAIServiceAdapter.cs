namespace AICentral.PipelineComponents.Endpoints.OpenAI;

public interface IAIServiceAdapter
{
    Task ApplyAuthToRequest(HttpRequest incomingRequest, HttpRequestMessage newRequest);
    object WriteDebug();
}