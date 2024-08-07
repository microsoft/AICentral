using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI.Authorisers;

public class KeyAuth : IEndpointAuthorisationHandler
{
    private readonly string _apiKey;

    public KeyAuth(string apiKey)
    {
        _apiKey = apiKey;
    }

    public Task ApplyAuthorisationToRequest(IRequestContext incomingRequest,
        HttpRequestMessage outgoingRequest)
    {
        outgoingRequest.Headers.Add("api-key", new[] { _apiKey });
        return Task.CompletedTask;
    }

    public object WriteDebug()
    {
        return new { Type = "ApiKey" };
    }
}