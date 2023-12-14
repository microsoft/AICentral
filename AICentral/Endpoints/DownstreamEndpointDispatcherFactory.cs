using AICentral.Core;

namespace AICentral.Endpoints;

public class DownstreamEndpointDispatcherFactory : IAICentralEndpointDispatcherFactory
{
    private readonly IEndpointRequestResponseHandlerFactory _endpointDispatcherFactory;

    public DownstreamEndpointDispatcherFactory(IEndpointRequestResponseHandlerFactory endpointDispatcherFactory)
    {
        _endpointDispatcherFactory = endpointDispatcherFactory;
    }
    
    public IAICentralEndpointDispatcher Build()
    {
        return new DownstreamEndpointDispatcher(_endpointDispatcherFactory.Build());
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