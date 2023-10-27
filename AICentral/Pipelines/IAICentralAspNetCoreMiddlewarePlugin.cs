namespace AICentral.Pipelines;

public interface IAICentralAspNetCoreMiddlewarePlugin
{
    void RegisterServices(IServiceCollection services);

    void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route);

    static virtual string ConfigName => throw new NotImplementedException();
    
    object WriteDebug();

}