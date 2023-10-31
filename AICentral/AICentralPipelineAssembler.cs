using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Auth;
using AICentral.PipelineComponents.Auth.AllowAnonymous;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.EndpointSelectors;
using AICentral.PipelineComponents.Routes;

namespace AICentral;

/// <summary>
/// Responsible for assembling the pipelines from all the builder representations.
/// This class will also Add all the required services needed for the pipelines.
/// </summary>
public class AICentralPipelineAssembler
{
    private readonly Dictionary<string, Func<Dictionary<string, string>, IAICentralRouter>> _routeBuilders;
    private readonly Dictionary<string, IAICentralClientAuthBuilder> _authProviders;
    private readonly Dictionary<string, IAiCentralEndpointDispatcherBuilder> _endpoints;
    private readonly Dictionary<string, IAICentralEndpointSelectorBuilder> _endpointSelectors;
    private readonly Dictionary<string, IAICentralPipelineStepBuilder<IAICentralPipelineStep>> _genericSteps;
    private readonly ConfigurationTypes.AICentralPipelineConfig[] _configPipelines;
    private Dictionary<IAICentralClientAuthBuilder, IAICentralClientAuthStep> _builtAuthProviders;
    private Dictionary<IAiCentralEndpointDispatcherBuilder, IAICentralEndpointDispatcher> _builtEndpoints;

    private Dictionary<IAICentralPipelineStepBuilder<IAICentralPipelineStep>, IAICentralPipelineStep>
        _builtSteps;

    private Dictionary<IAICentralEndpointSelectorBuilder, IAICentralEndpointSelector> _builtEndpointSelectors;
    private bool _built;

    public AICentralPipelineAssembler(
        Dictionary<string, Func<Dictionary<string, string>, IAICentralRouter>> routeBuilders,
        Dictionary<string, IAICentralClientAuthBuilder> authProviders,
        Dictionary<string, IAiCentralEndpointDispatcherBuilder> endpoints,
        Dictionary<string, IAICentralEndpointSelectorBuilder> endpointSelectors,
        Dictionary<string, IAICentralPipelineStepBuilder<IAICentralPipelineStep>> genericSteps,
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
        where TType : IAICentralPipelineStepBuilder<TRuntimeType> where TRuntimeType : IAICentralPipelineStep
    {
        if (string.IsNullOrEmpty(providerName))
            return fallback ?? throw new ArgumentException($"Can not find pipeline step {providerName}");

        return providers.TryGetValue(providerName, out var provider)
            ? runtime[provider]
            : throw new ArgumentException(
                $"Can not satisfy request for middleware {providerName}. Did you forget to configure it?");
    }

    private static IAICentralEndpointSelector GetEndpointSelector(
        Dictionary<string, IAICentralEndpointSelectorBuilder> providers,
        Dictionary<IAICentralEndpointSelectorBuilder, IAICentralEndpointSelector> runtime,
        string? providerName,
        IAICentralEndpointSelector? fallback)
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
                        new AllowAnonymousClientAuthProvider()),
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