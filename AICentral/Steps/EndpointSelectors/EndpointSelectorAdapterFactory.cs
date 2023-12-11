using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors;

public class EndpointSelectorAdapterFactory : IAICentralEndpointDispatcherFactory
{
    private readonly Lazy<EndpointSelectorAdapter> _instance;
    private readonly IAICentralEndpointSelectorFactory _centralEndpointSelectorFactory;

    public EndpointSelectorAdapterFactory(IAICentralEndpointSelectorFactory centralEndpointSelectorFactory)
    {
        _centralEndpointSelectorFactory = centralEndpointSelectorFactory;
        _instance = new Lazy<EndpointSelectorAdapter>(() => new EndpointSelectorAdapter(centralEndpointSelectorFactory));
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

    public static IAICentralEndpointDispatcherFactory BuildFromConfig(ILogger logger, IConfigurationSection configurationSection)
    {
        throw new NotImplementedException();
    }
}