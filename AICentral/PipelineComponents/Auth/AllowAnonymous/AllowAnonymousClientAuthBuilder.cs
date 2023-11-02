namespace AICentral.PipelineComponents.Auth.AllowAnonymous;

public class AllowAnonymousClientAuthBuilder: IAICentralClientAuthBuilder
{
    public void RegisterServices(IServiceCollection services)
    {
    }

    public IAICentralClientAuthStep Build()
    {
        return new AllowAnonymousClientAuthProvider();
    }

    public static IAICentralClientAuthBuilder BuildFromConfig(IConfigurationSection configurationSection, Dictionary<string, string> parameters)
    {
        return new AllowAnonymousClientAuthBuilder();
    }

    public static string ConfigName => "AllowAnonymous";
}

