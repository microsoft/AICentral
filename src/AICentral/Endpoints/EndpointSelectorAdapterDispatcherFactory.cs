using AICentral.Core;

namespace AICentral.Endpoints;

public class EndpointSelectorAdapterDispatcherFactory : IEndpointDispatcherFactory
{
    private readonly Lazy<EndpointSelectorAdapterDispatcher> _instance;
    private readonly IEndpointSelectorFactory _centralEndpointSelectorFactory;

    public EndpointSelectorAdapterDispatcherFactory(IEndpointSelectorFactory centralEndpointSelectorFactory)
    {
        _centralEndpointSelectorFactory = centralEndpointSelectorFactory;
        _instance = new Lazy<EndpointSelectorAdapterDispatcher>(() => new EndpointSelectorAdapterDispatcher(centralEndpointSelectorFactory));
    }

    public void RegisterServices(HttpMessageHandler? httpMessageHandler, IServiceCollection services)
    {
    }

    public IEndpointDispatcher Build()
    {
        return _instance.Value;
    }

    public object WriteDebug()
    {
        return _centralEndpointSelectorFactory.WriteDebug();
    }

    public static string ConfigName => "__internal_use_only";

}