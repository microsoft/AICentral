using AICentral.Core;

namespace AICentral.Endpoints;

public class DownstreamEndpointDispatcherFactory : IAICentralEndpointDispatcherFactory
{
    private readonly IDownstreamEndpointAdapterFactory _downstreamEndpointDispatcher;

    public DownstreamEndpointDispatcherFactory(IDownstreamEndpointAdapterFactory downstreamEndpointDispatcher)
    {
        _downstreamEndpointDispatcher = downstreamEndpointDispatcher;
    }
    
    public IAICentralEndpointDispatcher Build()
    {
        return new DownstreamEndpointDispatcher(_downstreamEndpointDispatcher.Build());
    }

    public object WriteDebug()
    {
        return _downstreamEndpointDispatcher.WriteDebug();
    }

    public void RegisterServices(HttpMessageHandler? optionalHandler, IServiceCollection services)
    {
        _downstreamEndpointDispatcher.RegisterServices(optionalHandler, services);
    }
}