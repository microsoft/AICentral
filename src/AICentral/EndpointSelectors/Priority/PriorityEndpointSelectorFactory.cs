using AICentral.Core;

namespace AICentral.EndpointSelectors.Priority;

public class PriorityEndpointSelectorFactory : IEndpointSelectorFactory
{
    private readonly IEndpointDispatcherFactory[] _prioritisedOpenAIEndpoints;
    private readonly IEndpointDispatcherFactory[] _fallbackOpenAIEndpoints;
    private readonly Lazy<PriorityEndpointSelector> _endpointSelector;

    public PriorityEndpointSelectorFactory(
        IEndpointDispatcherFactory[] prioritisedOpenAIEndpoints,
        IEndpointDispatcherFactory[] fallbackOpenAIEndpoints)
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

    public static IEndpointSelectorFactory BuildFromConfig(
        ILogger logger,
        TypeAndNameConfig config,
        Dictionary<string, IEndpointDispatcherFactory> endpoints)
    {
        var properties = config.TypedProperties<PriorityEndpointConfig>();

        var prioritisedEndpoints =
            Guard.NotNull(
                    properties.PriorityEndpoints,
                    nameof(properties.PriorityEndpoints))
                .Select(x =>
                    endpoints.TryGetValue(x, out var ep)
                        ? ep
                        : Guard.NotNull(ep,  nameof(properties.PriorityEndpoints)));

        var fallbackEndpoints =
            Guard.NotNull(
                    properties.FallbackEndpoints,
                    nameof(properties.FallbackEndpoints))
                .Select(x => endpoints.TryGetValue(x, out var ep) ? ep : Guard.NotNull(ep, nameof(properties.FallbackEndpoints)));

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