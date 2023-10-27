namespace AICentral.Pipelines.Auth;

public interface IAICentralClientAuthProvider : IAICentralAspNetCoreMiddlewarePlugin
{
    static virtual IAICentralClientAuthProvider BuildFromConfig(IConfigurationSection configurationSection, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }
}