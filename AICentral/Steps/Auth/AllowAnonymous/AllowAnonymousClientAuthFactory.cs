namespace AICentral.Steps.Auth.AllowAnonymous;

public class AllowAnonymousClientAuthFactory: IAICentralClientAuthFactory
{
    public void RegisterServices(IServiceCollection services)
    {
    }

    public IAICentralClientAuthStep Build()
    {
        return new AllowAnonymousClientAuthProvider();
    }

    public static IAICentralClientAuthFactory BuildFromConfig(ILogger logger, IConfigurationSection configurationSection)
    {
        return new AllowAnonymousClientAuthFactory();
    }

    public static string ConfigName => "AllowAnonymous";
}

