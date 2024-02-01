using AICentral.Core;

namespace AICentral.ConsumerAuth.AllowAnonymous;

public class AllowAnonymousClientAuthFactory: IPipelineStepFactory
{
   
    public void RegisterServices(IServiceCollection services)
    {
    }

    public IPipelineStep Build(IServiceProvider serviceProvider)
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

    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        return new AllowAnonymousClientAuthFactory();
    }

    public static string ConfigName => "AllowAnonymous";
}

