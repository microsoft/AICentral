namespace AICentral.Core;

public interface IRouteProxy
{
    RouteHandlerBuilder MapRoute(WebApplication application, AIHandler handler);
}
