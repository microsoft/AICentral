using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI.Authorisers;

public class KeyAuthFactory : IEndpointAuthorisationHandlerFactory
{
    private readonly string _apiKey;

    public KeyAuthFactory(string apiKey)
    {
        _apiKey = apiKey;
    }

    public static string ConfigName => "apikey";

    public IEndpointAuthorisationHandler Build()
    {
        return new KeyAuth(_apiKey);
    }

    public object WriteDebug()
    {
        return new { Type = "ApiKey" };
    }}