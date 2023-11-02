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
        IConfigurationSection configSection,
        Dictionary<string, IAICentralEndpointDispatcherBuilder> endpoints)
    {
        var config = configSection.Get<ConfigurationTypes.RandomEndpointConfig>()!;

        return new RandomEndpointSelectorBuilder(
            Guard.NotNull(config.Endpoints, configSection, "Endpoints")
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, configSection, "Endpoint"))
                .ToArray());
    }
}