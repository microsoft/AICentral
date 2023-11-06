using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Endpoints.AzureOpenAI;

namespace AICentral.PipelineComponents.Routes;

public class PathMatchRouter
{
    private readonly string _path;

    public PathMatchRouter(string path)
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

    public static PathMatchRouter WithPath(string path)
    {
        return new PathMatchRouter(Guard.NotNullOrEmptyOrWhitespace(path, nameof(path)));
    }
}