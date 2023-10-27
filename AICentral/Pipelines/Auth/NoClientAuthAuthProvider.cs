namespace AICentral.Pipelines.Auth;

public class NoClientAuthAuthProvider: IAICentralClientAuthProvider
{
    public void RegisterServices(IServiceCollection services)
    {
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        //no-op
    }

    public static IAICentralClientAuthProvider BuildFromConfig(IConfigurationSection configurationSection, Dictionary<string, string> parameters)
    {
        return new NoClientAuthAuthProvider();
    }

    public object WriteDebug()
    {
        return new { };
    }

    public static string ConfigName => "NoOp";
}