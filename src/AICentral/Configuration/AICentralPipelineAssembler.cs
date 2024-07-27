using AICentral.Core;
using AICentral.Endpoints;

namespace AICentral.Configuration;

/// <summary>
/// Responsible for assembling the pipelines from all the factories.
/// This class will also Add all the required services needed for the pipelines.
/// </summary>
public class AICentralPipelineAssembler
{
    private readonly Func<string, HostNameMatchRouter> _routeBuilder;
    private readonly Dictionary<string, IPipelineStepFactory> _authProviders;
    private readonly Dictionary<string, IEndpointDispatcherFactory> _endpoints;
    private readonly Dictionary<string, IEndpointSelectorFactory> _endpointSelectors;
    private readonly Dictionary<string, IPipelineStepFactory> _genericSteps;
    private readonly Dictionary<string, IRouteProxy> _routeProxies;
    private readonly PipelineConfig[] _pipelines;

    private bool _servicesAdded;

    public AICentralPipelineAssembler(
        Func<string, HostNameMatchRouter> routeBuilder,
        Dictionary<string, IPipelineStepFactory> authProviders,
        Dictionary<string, IEndpointDispatcherFactory> endpoints,
        Dictionary<string, IEndpointSelectorFactory> endpointSelectors,
        Dictionary<string, IPipelineStepFactory> genericSteps,
        Dictionary<string, IRouteProxy> routeProxies,
        PipelineConfig[] pipelines)
    {
        _routeBuilder = routeBuilder;
        _authProviders = authProviders;
        _endpoints = endpoints;
        _endpointSelectors = endpointSelectors;
        _genericSteps = genericSteps;
        _routeProxies = routeProxies;
        _pipelines = pipelines;
    }

    public ConfiguredPipelines AddServices(
        IServiceCollection services,
        HttpMessageHandler? optionalHandler,
        ILogger startupLogger)
    {
        _servicesAdded = _servicesAdded ? throw new InvalidOperationException("AICentral is already built") : true;

        services.AddSingleton<DateTimeProvider>();
        services.AddSingleton<DownstreamEndpointResponseDataTracker>();

        foreach (var authProvider in _authProviders) authProvider.Value.RegisterServices(services);
        foreach (var endpoint in _endpoints) endpoint.Value.RegisterServices(optionalHandler, services);
        foreach (var endpointSelector in _endpointSelectors) endpointSelector.Value.RegisterServices(services);
        foreach (var step in _genericSteps) step.Value.RegisterServices(services);

        var pipelines = BuildPipelines(startupLogger);
        services.AddSingleton(pipelines);
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return pipelines;
    }

    private ConfiguredPipelines BuildPipelines(ILogger startupLogger)
    {
        if (!_servicesAdded)
            throw new InvalidOperationException(
                "You must call AddServices on the Assembler before calling BuildPipelines.");

        var dupeCheck = new HashSet<string>();

        var pipelines = _pipelines
            .Select(pipelineConfig =>
            {
                var pipelineName =
                    Guard.NotNullOrEmptyOrWhitespace(pipelineConfig.Name, nameof(pipelineConfig.Name));

                var pipelineSteps = pipelineConfig.Steps ?? [];
                var routeProxies = pipelineConfig.RouteProxies ?? [];

                var routeBuilder =
                    _routeBuilder(
                        Guard.NotNullOrEmptyOrWhitespace(pipelineConfig.Host, nameof(pipelineConfig.Host)));

                startupLogger.LogInformation("Configuring Pipeline {Name} on Host {Host}", pipelineConfig.Name,
                    pipelineConfig.Host);

                if (!dupeCheck.Add(pipelineConfig.Host ?? string.Empty))
                {
                    startupLogger.LogWarning($"Duplicate Host {pipelineConfig.Host}. Ignoring pipeline {pipelineConfig.Name}");
                    return null;
                }

                var pipeline = new Pipeline(
                    pipelineName,
                    routeBuilder,
                    _authProviders.ContainsKey(pipelineConfig.AuthProvider ??
                                               throw new ArgumentException(
                                                   $"No AuthProvider for pipeline {pipelineConfig.Name}"))
                        ? _authProviders[pipelineConfig.AuthProvider ?? string.Empty]
                        : throw new ArgumentException($"Cannot find Auth Provider {pipelineConfig.AuthProvider}"),
                    pipelineSteps.Select(step =>
                        _genericSteps.TryGetValue(step, out var genericStep)
                            ? genericStep
                            : throw new ArgumentException($"Cannot find Step {step}")).ToArray(),
                    _endpointSelectors.ContainsKey(
                        pipelineConfig.EndpointSelector ??
                        throw new ArgumentException($"No EndpointSelector for pipeline {pipelineConfig.Name}"))
                        ? _endpointSelectors[pipelineConfig.EndpointSelector ?? string.Empty]
                        : throw new ArgumentException(
                            $"Cannot find EndpointSelector {pipelineConfig.EndpointSelector}"),
                    routeProxies.Select(proxy =>
                        _routeProxies.TryGetValue(proxy, out var routeProxy)
                            ? routeProxy
                            : throw new ArgumentException($"Cannot find RouteProxy {proxy}")).ToArray(),
                    pipelineConfig.OpenTelemetryConfig ?? new OTelConfig()
                    {
                        AddClientNameTag = false,
                        Transmit = false
                    });

                startupLogger.LogInformation("Configured Pipeline {Name} on Host {Host}", pipelineConfig.Name,
                    pipelineConfig.Host);

                return pipeline;
            }).Where(x => x != null).Select(x => x!).ToArray();
        
        return new ConfiguredPipelines(pipelines);
    }

    /// <summary>
    /// For tests
    /// </summary>
    /// <param name="otherAssembler"></param>
    /// <returns></returns>
    public AICentralPipelineAssembler CombineAssemblers(AICentralPipelineAssembler otherAssembler)
    {
        return new AICentralPipelineAssembler(
            _routeBuilder,
            otherAssembler._authProviders.Union(_authProviders).ToDictionary(x => x.Key, x => x.Value),
            otherAssembler._endpoints.Union(_endpoints).ToDictionary(x => x.Key, x => x.Value),
            otherAssembler._endpointSelectors.Union(_endpointSelectors).ToDictionary(x => x.Key, x => x.Value),
            otherAssembler._genericSteps.Union(_genericSteps).ToDictionary(x => x.Key, x => x.Value),
            otherAssembler._routeProxies.Union(_routeProxies).ToDictionary(x => x.Key, x => x.Value),
            otherAssembler._pipelines.Union(_pipelines).ToArray()
        );
    }
}
