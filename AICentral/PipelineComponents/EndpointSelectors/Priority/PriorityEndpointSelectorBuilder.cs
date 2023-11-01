using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.EndpointSelectors.Random;

namespace AICentral.PipelineComponents.EndpointSelectors.Priority;

public class PriorityEndpointSelectorBuilder : IAICentralEndpointSelectorBuilder
{
    private readonly RandomEndpointSelectorBuilder _prioritisedOpenAiEndpoints;
    private readonly RandomEndpointSelectorBuilder _fallbackOpenAiEndpoints;

    public PriorityEndpointSelectorBuilder(RandomEndpointSelectorBuilder prioritisedOpenAiEndpoints,
        RandomEndpointSelectorBuilder fallbackOpenAiEndpoints)
    {
        _prioritisedOpenAiEndpoints = prioritisedOpenAiEndpoints;
        _fallbackOpenAiEndpoints = fallbackOpenAiEndpoints;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "Prioritised";

    public static IAICentralEndpointSelectorBuilder BuildFromConfig(Dictionary<string, string> parameters,
        Dictionary<string, IAICentralEndpointDispatcherBuilder> endpoints)
    {
        return new PriorityEndpointSelectorBuilder(
            new RandomEndpointSelectorBuilder(
                parameters["PrioritisedEndpoints"].Split(',').Select(x => endpoints[x]).ToArray()),
            new RandomEndpointSelectorBuilder(parameters["FallbackEndpoints"].Split(',').Select(x => endpoints[x]).ToArray()));
    }

    public IAICentralEndpointSelector Build(Dictionary<IAICentralEndpointDispatcherBuilder, IAICentralEndpointDispatcher> builtEndpointDictionary)
    {
        return new PriorityEndpointSelector(
            (RandomEndpointSelector)_prioritisedOpenAiEndpoints.Build(builtEndpointDictionary),
            (RandomEndpointSelector)_fallbackOpenAiEndpoints.Build(builtEndpointDictionary));
    }
}