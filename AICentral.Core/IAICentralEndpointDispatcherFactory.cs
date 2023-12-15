namespace AICentral.Core;

public interface IAICentralEndpointDispatcherFactory
{
    IAICentralEndpointDispatcher Build();
    object WriteDebug();
    void RegisterServices(HttpMessageHandler? optionalHandler, IServiceCollection services);
}