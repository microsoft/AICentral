using AICentral.Core;

namespace AICentral.Steps.Auth.AllowAnonymous;

public class AllowAnonymousClientAuthFactory: IAICentralClientAuthFactory
{
   
    public void RegisterServices(IServiceCollection services)
    {
    }

    public IAICentralClientAuthStep Build()
    {
        return AllowAnonymousClientAuthProvider.Instance;
    }
    
    public object WriteDebug()
    {
        return new { auth = "No Consumer Auth" };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        //No-op
    }

    public static IAICentralClientAuthFactory BuildFromConfig(ILogger logger, AICentralTypeAndNameConfig config)
    {
        return new AllowAnonymousClientAuthFactory();
    }

    public static string ConfigName => "AllowAnonymous";
}

