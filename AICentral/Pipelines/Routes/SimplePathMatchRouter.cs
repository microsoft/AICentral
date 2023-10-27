namespace AICentral.Pipelines.Routes;

public class SimplePathMatchRouter: IAICentralRouter
{
    private readonly string _path;

    public SimplePathMatchRouter(string path)
    {
        _path = path;
    }

    public object WriteDebug()
    {
        return new { Path = _path };
    }

    public RouteHandlerBuilder BuildRoute(WebApplication application, Delegate handler)
    {
        return application.MapPost(_path, handler);
    }

    public static string ConfigName => "PathMatch";
    
    public static IAICentralRouter BuildFromConfig(Dictionary<string, string> parameters)
    {
        return new SimplePathMatchRouter(parameters["Path"]);
    }
}