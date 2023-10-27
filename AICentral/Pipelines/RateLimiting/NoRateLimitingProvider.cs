namespace AICentral.Pipelines.RateLimiting;

public class NoRateLimitingProvider : IAICentralRateLimitingProvider
{
    public void RegisterServices(IServiceCollection services)
    {
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        //no-op
    }

    public static IAICentralRateLimitingProvider BuildFromConfig(IConfigurationSection configurationSection,
        Dictionary<string, string> parameters)
    {
        return new NoRateLimitingProvider();
    }

    public object WriteDebug()
    {
        return new { };
    }

    public static string ConfigName => "NoOp";
}