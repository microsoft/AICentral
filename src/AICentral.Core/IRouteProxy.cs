namespace AICentral.Core;

public interface IRouteProxy
{
    RouteHandlerBuilder MapRoute(WebApplication application, AIHandler handler);
    
    static virtual string ConfigName  => throw new NotImplementedException();

    static virtual IRouteProxy BuildFromConfig(ILogger logger, TypeAndNameConfig config) => throw new NotImplementedException();

    object WriteDebug();

}
