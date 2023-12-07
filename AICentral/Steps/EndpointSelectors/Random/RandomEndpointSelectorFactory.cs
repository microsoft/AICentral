using AICentral.Configuration.JSON;
using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors.Random;

public class RandomEndpointSelectorFactory : IAICentralEndpointSelectorFactory
{
    private readonly IAICentralEndpointDispatcherFactory[] _openAiServers;
    private readonly Lazy<RandomEndpointSelector> _endpointSelector;

    public RandomEndpointSelectorFactory(IAICentralEndpointDispatcherFactory[] openAiServers)
    {
        _openAiServers = openAiServers.ToArray();
        _endpointSelector = new Lazy<RandomEndpointSelector>(() => new RandomEndpointSelector(_openAiServers.Select(x => x.Build()).ToArray()));
    }

    public IEndpointSelector Build()
    {
        return _endpointSelector.Value;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "RandomCluster";

    public static IAICentralEndpointSelectorFactory BuildFromConfig(
        ILogger logger, 
        IConfigurationSection configurationSection,
        Dictionary<string, IAICentralEndpointDispatcherFactory> endpoints)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.RandomEndpointConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");

        return new RandomEndpointSelectorFactory(
            Guard.NotNull(properties!.Endpoints, configurationSection, "Endpoints")
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, configurationSection, "Endpoint"))
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