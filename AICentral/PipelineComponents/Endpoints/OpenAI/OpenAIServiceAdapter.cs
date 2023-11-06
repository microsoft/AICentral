using System.Net.Http.Headers;

namespace AICentral.PipelineComponents.Endpoints.OpenAI;

public class OpenAIServiceAdapter : IAIServiceAdapter
{
    private readonly string _apiKey;

    public OpenAIServiceAdapter(string apiKey)
    {
        _apiKey = apiKey;
    }

    public Task ApplyAuthToRequest(HttpRequest incomingRequest, HttpRequestMessage newRequest)
    {
        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return Task.CompletedTask;
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "Open AI"
        };
    }
}