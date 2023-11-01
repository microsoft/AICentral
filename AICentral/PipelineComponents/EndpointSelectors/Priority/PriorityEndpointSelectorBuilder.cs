using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.EndpointSelectors.Random;

namespace AICentral.PipelineComponents.EndpointSelectors.Priority;

public class PriorityEndpointSelectorBuilder : IAICentralEndpointSelectorBuilder
{
    private readonly IAICentralEndpointDispatcherBuilder[] _prioritisedOpenAiEndpoints;
    private readonly IAICentralEndpointDispatcherBuilder[] _fallbackOpenAiEndpoints;

    public PriorityEndpointSelectorBuilder(
        IAICentralEndpointDispatcherBuilder[] prioritisedOpenAiEndpoints,
        IAICentralEndpointDispatcherBuilder[] fallbackOpenAiEndpoints)
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
            parameters["PrioritisedEndpoints"].Split(',').Select(x => endpoints[x]).ToArray(),
            parameters["FallbackEndpoints"].Split(',').Select(x => endpoints[x]).ToArray()
        );
    }

    public IEndpointSelector Build(
        Dictionary<IAICentralEndpointDispatcherBuilder, IAICentralEndpointDispatcher> builtEndpointDictionary)
    {
        return new PriorityEndpointSelector(
            _prioritisedOpenAiEndpoints.Select(x => builtEndpointDictionary[x]).ToArray(),
            _fallbackOpenAiEndpoints.Select(x => builtEndpointDictionary[x]).ToArray());
    }
}