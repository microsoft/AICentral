using AICentral.PipelineComponents.Endpoints;

namespace AICentral.PipelineComponents.EndpointSelectors.Random;

public class RandomEndpointSelectorBuilder : IAICentralEndpointSelectorBuilder
{
    private readonly IAiCentralEndpointDispatcherBuilder[] _openAiServers;

    public RandomEndpointSelectorBuilder(IList<IAiCentralEndpointDispatcherBuilder> openAiServers)
    {
        _openAiServers = openAiServers.ToArray();
    }

    public IAICentralEndpointSelector Build(
        Dictionary<IAiCentralEndpointDispatcherBuilder, IAICentralEndpointDispatcher> builtEndpointDictionary)
    {
        return new RandomEndpointSelector(_openAiServers.Select(x => builtEndpointDictionary[x]).ToArray());
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "RoundRobinCluster";

    public static IAICentralEndpointSelectorBuilder BuildFromConfig(Dictionary<string, string> parameters,
        Dictionary<string, IAiCentralEndpointDispatcherBuilder> endpoints)
    {
        return new RandomEndpointSelectorBuilder(
            parameters["Endpoints"].Split(',').Select(x => endpoints[x])
                .ToArray());
    }
}