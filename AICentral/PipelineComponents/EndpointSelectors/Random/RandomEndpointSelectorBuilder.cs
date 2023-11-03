using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.OpenAI;

namespace AICentral.PipelineComponents.EndpointSelectors.Random;

public class RandomEndpointSelectorBuilder : IAICentralEndpointSelectorBuilder
{
    private readonly IAICentralEndpointDispatcherBuilder[] _openAiServers;

    public RandomEndpointSelectorBuilder(IAICentralEndpointDispatcherBuilder[] openAiServers)
    {
        _openAiServers = openAiServers.ToArray();
    }

    public IEndpointSelector Build(
        Dictionary<IAICentralEndpointDispatcherBuilder, IAICentralEndpointDispatcher> builtEndpointDictionary)
    {
        return new RandomEndpointSelector(_openAiServers.Select(x => builtEndpointDictionary[x]).ToArray());
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "RandomCluster";

    public static IAICentralEndpointSelectorBuilder BuildFromConfig(
        IConfigurationSection configurationSection,
        Dictionary<string, IAICentralEndpointDispatcherBuilder> endpoints)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.RandomEndpointConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");

        return new RandomEndpointSelectorBuilder(
            Guard.NotNull(properties!.Endpoints, configurationSection, "Endpoints")
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, configurationSection, "Endpoint"))
                .ToArray());
    }
}