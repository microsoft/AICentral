using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI.Authorisers;

public class EntraAuthFactory : IEndpointAuthorisationHandlerFactory
{
    public static string ConfigName => "entra";

    public IEndpointAuthorisationHandler Build()
    {
        return new EntraAuth();
    }

    public object WriteDebug()
    {
        return new { Type = "Entra Auth Using Managed Identity" };
    }
}