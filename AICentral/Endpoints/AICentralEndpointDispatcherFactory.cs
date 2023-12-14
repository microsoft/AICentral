using AICentral.Core;

namespace AICentral.Endpoints;

public class AICentralEndpointDispatcherFactory : IAICentralEndpointDispatcherFactory
{
    private readonly IEndpointRequestResponseHandlerFactory _endpointDispatcherFactory;

    public AICentralEndpointDispatcherFactory(IEndpointRequestResponseHandlerFactory endpointDispatcherFactory)
    {
        _endpointDispatcherFactory = endpointDispatcherFactory;
    }
    
    public IAICentralEndpointDispatcher Build()
    {
        return new AICentralEndpointDispatcher(_endpointDispatcherFactory.Build());
    }

    public object WriteDebug()
    {
        return _endpointDispatcherFactory.WriteDebug();
    }

    public void RegisterServices(HttpMessageHandler? optionalHandler, IServiceCollection services)
    {
        _endpointDispatcherFactory.RegisterServices(optionalHandler, services);
    }
}