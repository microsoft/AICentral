using AICentral.Configuration.JSON;
using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors.LowestLatency;

public class LowestLatencyEndpointSelectorBuilder : IAICentralEndpointSelectorBuilder
{
    private readonly IAICentralEndpointDispatcherBuilder[] _openAiServers;

    public LowestLatencyEndpointSelectorBuilder(IAICentralEndpointDispatcherBuilder[] openAiServers)
    {
        _openAiServers = openAiServers.ToArray();
    }

    public IEndpointSelector Build(
        Dictionary<IAICentralEndpointDispatcherBuilder, IAICentralEndpointDispatcher> builtEndpointDictionary)
    {
        return new LowestLatencyEndpointSelector(_openAiServers.Select(x => builtEndpointDictionary[x]).ToArray());
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "LowestLatency";

    public static IAICentralEndpointSelectorBuilder BuildFromConfig(
        ILogger logger, 
        IConfigurationSection configurationSection,
        Dictionary<string, IAICentralEndpointDispatcherBuilder> endpoints)
    {
        var properties = configurationSection.GetSection("Properties").Get<LowestLatencyEndpointConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");

        return new LowestLatencyEndpointSelectorBuilder(
            Guard.NotNull(properties!.Endpoints, configurationSection, "Endpoints")
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, configurationSection, "Endpoint"))
                .ToArray());
    }
}