namespace AICentral.Pipelines.Routes;

public interface IAICentralRouter
{
    static virtual string ConfigName => throw new NotImplementedException();

    static virtual IAICentralRouter BuildFromConfig(Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }

    RouteHandlerBuilder BuildRoute(WebApplication application, Delegate handler);

    object WriteDebug();
}