using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.OpenAI;
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

    public static IAICentralEndpointSelectorBuilder BuildFromConfig(
        IConfigurationSection section,
        Dictionary<string, IAICentralEndpointDispatcherBuilder> endpoints)
    {
        if (!section.Exists()) throw new ArgumentException($"Missing configuration section {section.Path}");
        var config = section.Get<ConfigurationTypes.PriorityEndpointConfig>()!;

        var prioritisedEndpoints =
            Guard.NotNull(
                    config.PriorityEndpoints,
                    section,
                    nameof(config.PriorityEndpoints))
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, section, "PrioritisedEndpoint"));

        var fallbackEndpoints =
            Guard.NotNull(
                    config.FallbackEndpoints,
                    section,
                    nameof(config.FallbackEndpoints))
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, section, ""));

        return new PriorityEndpointSelectorBuilder(
            prioritisedEndpoints.ToArray(),
            fallbackEndpoints.ToArray()
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