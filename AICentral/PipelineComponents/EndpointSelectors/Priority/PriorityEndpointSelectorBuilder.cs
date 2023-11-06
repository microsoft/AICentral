using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.AzureOpenAI;
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
        IConfigurationSection configurationSection,
        Dictionary<string, IAICentralEndpointDispatcherBuilder> endpoints)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.PriorityEndpointConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");

        var prioritisedEndpoints =
            Guard.NotNull(
                    properties!.PriorityEndpoints,
                    configurationSection,
                    nameof(properties.PriorityEndpoints))
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, configurationSection, "PrioritisedEndpoint"));

        var fallbackEndpoints =
            Guard.NotNull(
                    properties.FallbackEndpoints,
                    configurationSection,
                    nameof(properties.FallbackEndpoints))
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, configurationSection, ""));

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