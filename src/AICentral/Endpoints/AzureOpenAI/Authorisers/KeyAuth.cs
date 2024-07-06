using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI;

public class KeyAuthFactory : IEndpointAuthorisationHandlerFactory
{
    private string _apiKey;

    public KeyAuthFactory(string apiKey)
    {
        _apiKey = apiKey;
    }

    public static string ConfigName => "entra";

    public void RegisterServices(IServiceCollection services)
    {
    }

    public IEndpointAuthorisationHandler Build()
    {
        return new KeyAuth(_apiKey);
    }

    public object WriteDebug()
    {
        return new { Type = "ApiKey" };
    }}


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