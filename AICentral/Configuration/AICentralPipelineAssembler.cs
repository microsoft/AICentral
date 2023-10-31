using AICentral.Configuration.JSON;
using AICentral.Pipelines;
using AICentral.Pipelines.Auth;
using AICentral.Pipelines.Endpoints;
using AICentral.Pipelines.EndpointSelectors;
using AICentral.Pipelines.Routes;

namespace AICentral.Configuration;

/// <summary>
/// Responsible for assembling the pipelines from all the component bits 
/// </summary>
public class AICentralPipelineAssembler
{
    private readonly Dictionary<string, Func<Dictionary<string, string>, IAICentralRouter>> _routeBuilders;
    private readonly Dictionary<string, IAICentralClientAuthProvider> _authProviders;
    private readonly Dictionary<string, IAICentralEndpoint> _endpoints;
    private readonly Dictionary<string, IAICentralEndpointSelector> _endpointSelectors;
    private readonly Dictionary<string, IAICentralPipelineStep<IAICentralPipelineStepRuntime>> _genericSteps;
    private readonly ConfigurationTypes.AICentralPipelineConfig[] _configPipelines;
    private Dictionary<IAICentralClientAuthProvider, IAICentralClientAuthRuntime> _builtAuthProviders;
    private Dictionary<IAICentralEndpoint, IAICentralEndpointRuntime> _builtEndpoints;

    private Dictionary<IAICentralPipelineStep<IAICentralPipelineStepRuntime>, IAICentralPipelineStepRuntime>
        _builtSteps;

    private Dictionary<IAICentralEndpointSelector, IAICentralEndpointSelectorRuntime> _builtEndpointSelectors;
    private bool _built;

    public AICentralPipelineAssembler(
        Dictionary<string, Func<Dictionary<string, string>, IAICentralRouter>> routeBuilders,
        Dictionary<string, IAICentralClientAuthProvider> authProviders,
        Dictionary<string, IAICentralEndpoint> endpoints,
        Dictionary<string, IAICentralEndpointSelector> endpointSelectors,
        Dictionary<string, IAICentralPipelineStep<IAICentralPipelineStepRuntime>> genericSteps,
        ConfigurationTypes.AICentralPipelineConfig[] configPipelines)
    {
        _routeBuilders = routeBuilders;
        _authProviders = authProviders;
        _endpoints = endpoints;
        _endpointSelectors = endpointSelectors;
        _genericSteps = genericSteps;
        _configPipelines = configPipelines;
    }

    public AICentralPipelines AddServices(IServiceCollection services, ILogger startupLogger)
    {
        _built = _built ? throw new InvalidOperationException("AICentral is already built") : true;

        foreach (var authProvider in _authProviders) authProvider.Value.RegisterServices(services);
        foreach (var endpoint in _endpoints) endpoint.Value.RegisterServices(services);
        foreach (var endpointSelector in _endpointSelectors) endpointSelector.Value.RegisterServices(services);
        foreach (var step in _genericSteps) step.Value.RegisterServices(services);

        _builtAuthProviders = _authProviders.ToDictionary(x => x.Value, x => x.Value.Build());
        _builtEndpoints = _endpoints.ToDictionary(x => x.Value, x => x.Value.Build());
        _builtSteps = _genericSteps.ToDictionary(x => x.Value, x => x.Value.Build());
        _builtEndpointSelectors = _endpointSelectors.ToDictionary(x => x.Value, x => x.Value.Build(_builtEndpoints));

        var pipelines = BuildPipelines(startupLogger);
        services.AddSingleton(pipelines);
        return pipelines;
    }


    private static TRuntimeType GetMiddlewareOrNoOp<TType, TRuntimeType>(
        Dictionary<string, TType> providers,
        Dictionary<TType, TRuntimeType> runtime,
        string? providerName,
        TRuntimeType? fallback)
        where TType : IAICentralPipelineStep<TRuntimeType> where TRuntimeType : IAICentralPipelineStepRuntime
    {
        if (string.IsNullOrEmpty(providerName))
            return fallback ?? throw new ArgumentException($"Can not find pipeline step {providerName}");

        return providers.TryGetValue(providerName, out var provider)
            ? runtime[provider]
            : throw new ArgumentException(
                $"Can not satisfy request for middleware {providerName}. Did you forget to configure it?");
    }

    private static IAICentralEndpointSelectorRuntime GetEndpointSelector(
        Dictionary<string, IAICentralEndpointSelector> providers,
        Dictionary<IAICentralEndpointSelector, IAICentralEndpointSelectorRuntime> runtime,
        string? providerName,
        IAICentralEndpointSelectorRuntime? fallback)
    {
        if (string.IsNullOrEmpty(providerName))
            return fallback ?? throw new ArgumentException($"Can not find pipeline step {providerName}");

        return providers.TryGetValue(providerName, out var provider)
            ? runtime[provider]
            : throw new ArgumentException(
                $"Can not satisfy request for middleware {providerName}. Did you forget to configure it?");
    }

    private AICentralPipelines BuildPipelines(ILogger startupLogger)
    {
        if (_configPipelines.Length == 0) throw new ArgumentException("No Pipelines specified in config");

        return new AICentralPipelines(
            
            _configPipelines.Select(pipelineConfig =>
            {
                startupLogger.LogInformation("Configuring Pipeline {Name} listening on Route {Route}",
                    pipelineConfig.Name,
                    pipelineConfig.Path);

                var pipelineName = string.IsNullOrEmpty(pipelineConfig.Name)
                    ? throw new ArgumentException("Missing Name for pipeline")
                    : pipelineConfig.Name!;

                var pipelineSteps = pipelineConfig.Steps ??
                                    throw new ArgumentException(
                                        $"No Pipelines steps specified in config for Pipeline {pipelineName}");

                var routeBuilder =
                    _routeBuilders[
                        (pipelineConfig.Path?.Type ?? throw new ArgumentException("Missing Path for pipeline"))](
                        pipelineConfig.Path.Properties ?? throw new ArgumentException("Missing properties for path"));

                var pipeline = new AICentralPipeline(
                    pipelineName,
                    routeBuilder,
                    GetMiddlewareOrNoOp(
                        _authProviders,
                        _builtAuthProviders,
                        pipelineConfig.AuthProvider,
                        new NoClientAuthAuthRuntime()),
                    pipelineSteps.Select(step => GetMiddlewareOrNoOp(
                        _genericSteps,
                        _builtSteps,
                        step,
                        default)).ToArray(),
                    GetEndpointSelector(
                        _endpointSelectors,
                        _builtEndpointSelectors,
                        pipelineConfig.EndpointSelector,
                        default));

                startupLogger.LogInformation("Configured Pipeline {Name} listening on Route {Route}",
                    pipelineConfig.Name,
                    pipelineConfig.Path);

                return pipeline;
            }).ToArray());
    }
}