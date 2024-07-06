using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI.Authorisers;

public class BearerTokenPassThroughAuthFactory : IEndpointAuthorisationHandlerFactory
{
    public static string ConfigName => "entrapassthrough";

    public void RegisterServices(IServiceCollection services)
    {
    }

    public IEndpointAuthorisationHandler Build()
    {
        return new BearerTokenPassThroughAuth();
    }

    public object WriteDebug()
    {
        return new
        {
            BackendAuth = "BearerTokenPassThrough"
        };
    }
}