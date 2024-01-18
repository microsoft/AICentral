using AICentral.Core;

namespace AICentral;

public class HeaderMatchRouter
{
    private readonly string _hostName;

    public HeaderMatchRouter(string hostName)
    {
        _hostName = hostName;
    }

    public object WriteDebug()
    {
        return new { Host = _hostName };
    }
    
    public RouteHandlerBuilder BuildRoute(WebApplication application, Delegate handler)
    {
        return application.MapMethods("{*:rest}", new[] { "Get", "Post" }, handler)
            .RequireHost(_hostName);
    }
    
    public static HeaderMatchRouter WithHostHeader(string host)
    {
        return new HeaderMatchRouter(Guard.NotNullOrEmptyOrWhitespace(host, nameof(host)));
    }
}