using AICentral.Core;
using AICentral.EndpointSelectors.Random;

namespace AICentral.EndpointSelectors.HighestCapacity;

public class HighestCapacitySelectorFactory : IEndpointSelectorFactory
{
    private readonly IEndpointDispatcherFactory[] _openAiServers;
    private readonly Lazy<HighestCapacitySelector> _endpointSelector;

    public HighestCapacitySelectorFactory(IEndpointDispatcherFactory[] openAiServers)
    {
        _openAiServers = openAiServers.ToArray();
        _endpointSelector = new Lazy<HighestCapacitySelector>(() => new HighestCapacitySelector(_openAiServers.Select(x => x.Build()).ToArray()));
    }

    public IEndpointSelector Build()
    {
        return _endpointSelector.Value;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "HighestCapacity";

    public static IEndpointSelectorFactory BuildFromConfig(
        ILogger logger, 
        TypeAndNameConfig config,
        Dictionary<string, IEndpointDispatcherFactory> endpoints)
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
            Endpoints = _openAiServers.Select(x => x.WriteDebug())
        };
    }
}