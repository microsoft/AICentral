namespace AICentral.Pipelines.RateLimiting;

public interface IAICentralRateLimitingProvider : IAICentralAspNetCoreMiddlewarePlugin
{
    static virtual IAICentralRateLimitingProvider BuildFromConfig(IConfigurationSection configurationSection, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }
}