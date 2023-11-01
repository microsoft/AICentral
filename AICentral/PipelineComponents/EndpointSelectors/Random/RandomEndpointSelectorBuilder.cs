using AICentral.PipelineComponents.Endpoints;

namespace AICentral.PipelineComponents.EndpointSelectors.Random;

public class RandomEndpointSelectorBuilder : IAICentralEndpointSelectorBuilder
{
    private readonly IAICentralEndpointDispatcherBuilder[] _openAiServers;

    public RandomEndpointSelectorBuilder(IList<IAICentralEndpointDispatcherBuilder> openAiServers)
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

    public static string ConfigName => "RoundRobinCluster";

    public static IAICentralEndpointSelectorBuilder BuildFromConfig(Dictionary<string, string> parameters,
        Dictionary<string, IAICentralEndpointDispatcherBuilder> endpoints)
    {
        return new RandomEndpointSelectorBuilder(
            parameters["Endpoints"].Split(',').Select(x => endpoints[x])
                .ToArray());
    }
}