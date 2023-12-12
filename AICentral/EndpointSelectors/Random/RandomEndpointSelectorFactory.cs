using AICentral.Core;

namespace AICentral.EndpointSelectors.Random;

public class RandomEndpointSelectorFactory : IAICentralEndpointSelectorFactory
{
    private readonly IAICentralEndpointDispatcherFactory[] _openAiServers;
    private readonly Lazy<RandomEndpointSelector> _endpointSelector;

    public RandomEndpointSelectorFactory(IAICentralEndpointDispatcherFactory[] openAiServers)
    {
        _openAiServers = openAiServers.ToArray();
        _endpointSelector = new Lazy<RandomEndpointSelector>(() => new RandomEndpointSelector(_openAiServers.Select(x => x.Build()).ToArray()));
    }

    public IAICentralEndpointSelector Build()
    {
        return _endpointSelector.Value;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "RandomCluster";

    public static IAICentralEndpointSelectorFactory BuildFromConfig(
        ILogger logger, 
        AICentralTypeAndNameConfig config,
        Dictionary<string, IAICentralEndpointDispatcherFactory> endpoints)
    {
        var properties = config.TypedProperties<RandomEndpointConfig>();
        Guard.NotNull(properties, "Properties");

        return new RandomEndpointSelectorFactory(
            Guard.NotNull(properties!.Endpoints, "Endpoints")
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, "Endpoint"))
                .ToArray());
    }
    
    public object WriteDebug()
    {
        return new
        {
            Type = "Random Router",
            Endpoints = _openAiServers.Select(x => WriteDebug())
        };
    }
}