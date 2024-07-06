using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI;

public class EntraAuthFactory : IEndpointAuthorisationHandlerFactory
{
    public static string ConfigName => "apikey";

    public void RegisterServices(IServiceCollection services)
    {
    }

    public IEndpointAuthorisationHandler Build()
    {
        return new EntraAuth();
    }

    public object WriteDebug()
    {
        return new { Type = "Entra Auth Using Managed Identity" };
    }
}