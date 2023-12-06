using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors.LowestLatency;

public class LowestLatencyEndpointSelectorFactory : IAICentralEndpointSelectorFactory
{
    private readonly IAICentralEndpointDispatcherFactory[] _openAiServers;
    private readonly Lazy<LowestLatencyEndpointSelector> _endpointSelector;

    public LowestLatencyEndpointSelectorFactory(IAICentralEndpointDispatcherFactory[] openAiServers)
    {
        _openAiServers = openAiServers.ToArray();
        _endpointSelector = new Lazy<LowestLatencyEndpointSelector>(() => new LowestLatencyEndpointSelector(
            _openAiServers.Select(x => x.Build()).ToArray()));
    }

    public IEndpointSelector Build()
    {
        return _endpointSelector.Value;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "LowestLatency";

    public static IAICentralEndpointSelectorFactory BuildFromConfig(
        ILogger logger, 
        IConfigurationSection configurationSection,
        Dictionary<string, IAICentralEndpointDispatcherFactory> endpoints,
        Dictionary<string, IAICentralEndpointSelectorFactory> endpointSelectors
        )
    {
        var properties = configurationSection.GetSection("Properties").Get<LowestLatencyEndpointConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");

        return new LowestLatencyEndpointSelectorFactory(
            Guard.NotNull(properties!.Endpoints, configurationSection, "Endpoints")
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, configurationSection, "Endpoint"))
                .ToArray());
    }
    
    
    public object WriteDebug()
    {
        return new
        {
            Type = "Lowest Latency Router",
            Endpoints = _openAiServers.Select(x => WriteDebug())
        };
    }
}