using AICentral.Core;

namespace AICentral.EndpointSelectors.LowestLatency;

public class LowestLatencyEndpointSelectorFactory : IEndpointSelectorFactory
{
    private readonly IEndpointDispatcherFactory[] _openAiServers;
    private readonly Lazy<LowestLatencyEndpointSelector> _endpointSelector;

    public LowestLatencyEndpointSelectorFactory(IEndpointDispatcherFactory[] openAiServers)
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

    public static IEndpointSelectorFactory BuildFromConfig(
        ILogger logger, 
        TypeAndNameConfig config,
        Dictionary<string, IEndpointDispatcherFactory> endpoints
        )
    {
        var properties = config.TypedProperties<LowestLatencyEndpointConfig>();
        Guard.NotNull(properties, "Properties");

        return new LowestLatencyEndpointSelectorFactory(
            Guard.NotNull(properties.Endpoints, "Endpoints")
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : throw new ArgumentException($"Cannot find Endpoint {x} in built endpoints"))
                .ToArray());
    }
    
    
    public object WriteDebug()
    {
        return new
        {
            Type = "Lowest Latency Router",
            Endpoints = _openAiServers.Select(x => x.WriteDebug())
        };
    }
}