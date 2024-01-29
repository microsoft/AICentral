using AICentral.Core;

namespace AICentral.ConsumerAuth.AllowAnonymous;

public class AllowAnonymousClientAuthFactory: IConsumerAuthFactory
{
   
    public void RegisterServices(IServiceCollection services)
    {
    }

    public IConsumerAuthStep Build(IServiceProvider serviceProvider)
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

    public static IConsumerAuthFactory BuildFromConfig(ILogger logger, AICentralTypeAndNameConfig config)
    {
        return new AllowAnonymousClientAuthFactory();
    }

    public static string ConfigName => "AllowAnonymous";
}

