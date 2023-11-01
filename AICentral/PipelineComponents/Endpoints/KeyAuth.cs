namespace AICentral.PipelineComponents.Endpoints;

public class KeyAuth : IEndpointAuthorisationHandler
{
    private readonly string _authenticationKey;

    public KeyAuth(string authenticationKey)
    {
        _authenticationKey = authenticationKey;
    }

    public Task ApplyAuthorisationToRequest(HttpRequest incomingRequest,
        HttpRequestMessage outgoingRequest)
    {
        outgoingRequest.Headers.Add("api-key", new[] { _authenticationKey });
        return Task.CompletedTask;
    }

    public object WriteDebug()
    {
        return new { Type = "ApiKey" };
    }
}