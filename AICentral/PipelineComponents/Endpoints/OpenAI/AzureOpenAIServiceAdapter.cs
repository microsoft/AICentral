namespace AICentral.PipelineComponents.Endpoints.OpenAI;

public class AzureOpenAIServiceAdapter: IAIServiceAdapter
{
    private readonly IEndpointAuthorisationHandler _endpointAuthorisationHandler;

    public AzureOpenAIServiceAdapter(IEndpointAuthorisationHandler endpointAuthorisationHandler)
    {
        _endpointAuthorisationHandler = endpointAuthorisationHandler;
    }

    public Task ApplyAuthToRequest(HttpRequest incomingRequest, HttpRequestMessage newRequest)
    {
        return _endpointAuthorisationHandler.ApplyAuthorisationToRequest(incomingRequest, newRequest);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "Azure Open AI",
            Auth = _endpointAuthorisationHandler.WriteDebug()
        };
    }
}