using AICentral.Configuration.JSON;
using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors.Priority;

public class PriorityEndpointSelectorFactory : IAICentralEndpointSelectorFactory
{
    private readonly IAICentralEndpointDispatcherFactory[] _prioritisedOpenAIEndpoints;
    private readonly IAICentralEndpointDispatcherFactory[] _fallbackOpenAIEndpoints;
    private Lazy<PriorityEndpointSelector> _endpointSelector;

    public PriorityEndpointSelectorFactory(
        IAICentralEndpointDispatcherFactory[] prioritisedOpenAIEndpoints,
        IAICentralEndpointDispatcherFactory[] fallbackOpenAIEndpoints)
    {
        _prioritisedOpenAIEndpoints = prioritisedOpenAIEndpoints;
        _fallbackOpenAIEndpoints = fallbackOpenAIEndpoints;
        _endpointSelector = new Lazy<PriorityEndpointSelector>(() => new PriorityEndpointSelector(
            _prioritisedOpenAIEndpoints.Select(x => x.Build()).ToArray(),
            _fallbackOpenAIEndpoints.Select(x => x.Build()).ToArray()));
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public static string ConfigName => "Prioritised";

    public static IAICentralEndpointSelectorFactory BuildFromConfig(
        ILogger logger,
        IConfigurationSection configurationSection,
        Dictionary<string, IAICentralEndpointDispatcherFactory> endpoints)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.PriorityEndpointConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");

        var prioritisedEndpoints =
            Guard.NotNull(
                    properties!.PriorityEndpoints,
                    configurationSection,
                    nameof(properties.PriorityEndpoints))
                .Select(x =>
                    endpoints.TryGetValue(x, out var ep)
                        ? ep
                        : Guard.NotNull(ep, configurationSection, "PrioritisedEndpoint"));

        var fallbackEndpoints =
            Guard.NotNull(
                    properties.FallbackEndpoints,
                    configurationSection,
                    nameof(properties.FallbackEndpoints))
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, configurationSection, ""));

        return new PriorityEndpointSelectorFactory(
            prioritisedEndpoints.ToArray(),
            fallbackEndpoints.ToArray()
        );
    }

    public IEndpointSelector Build()
    {
        return _endpointSelector.Value;
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "Priority Router",
            PrioritisedEndpoints = _prioritisedOpenAIEndpoints.Select(x => x.WriteDebug()),
            FallbackEndpoints = _fallbackOpenAIEndpoints.Select(x => x.WriteDebug()),
        };
    }
}