using AICentral.Core;

namespace AICentral.Endpoints;

public class EndpointSelectorAdapterDispatcherFactory : IAICentralEndpointDispatcherFactory
{
    private readonly Lazy<EndpointSelectorAdapterDispatcher> _instance;
    private readonly IAICentralEndpointSelectorFactory _centralEndpointSelectorFactory;

    public EndpointSelectorAdapterDispatcherFactory(IAICentralEndpointSelectorFactory centralEndpointSelectorFactory)
    {
        _centralEndpointSelectorFactory = centralEndpointSelectorFactory;
        _instance = new Lazy<EndpointSelectorAdapterDispatcher>(() => new EndpointSelectorAdapterDispatcher(centralEndpointSelectorFactory));
    }

    public void RegisterServices(HttpMessageHandler? httpMessageHandler, IServiceCollection services)
    {
    }

    public IAICentralEndpointDispatcher Build()
    {
        return _instance.Value;
    }

    public object WriteDebug()
    {
        return _centralEndpointSelectorFactory.WriteDebug();
    }

    public static string ConfigName => "__internal_use_only";

}