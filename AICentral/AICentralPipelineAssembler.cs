using AICentral.Configuration;
using AICentral.Configuration.JSON;
using AICentral.IncomingServiceDetector;
using AICentral.Steps;
using AICentral.Steps.Auth;
using AICentral.Steps.Auth.AllowAnonymous;
using AICentral.Steps.Endpoints;
using AICentral.Steps.EndpointSelectors;
using AICentral.Steps.Routes;

namespace AICentral;

/// <summary>
/// Responsible for assembling the pipelines from all the builder representations.
/// This class will also Add all the required services needed for the pipelines.
/// </summary>
public class AICentralPipelineAssembler
{
    private readonly Func<string, HeaderMatchRouter> _routeBuilder;
    private readonly Dictionary<string, IAICentralClientAuthBuilder> _authProviders;
    private readonly Dictionary<string, IAICentralEndpointDispatcherBuilder> _endpoints;
    private readonly Dictionary<string, IAICentralEndpointSelectorBuilder> _endpointSelectors;
    private readonly Dictionary<string, IAICentralPipelineStepBuilder<IAICentralPipelineStep>> _genericSteps;
    private readonly ConfigurationTypes.AICentralPipelineConfig[] _pipelines;

    private Dictionary<IAICentralClientAuthBuilder, IAICentralClientAuthStep>? _builtAuthProviders;
    private Dictionary<IAICentralEndpointDispatcherBuilder, IAICentralEndpointDispatcher>? _builtEndpoints;
    private Dictionary<IAICentralPipelineStepBuilder<IAICentralPipelineStep>, IAICentralPipelineStep>? _builtSteps;
    private Dictionary<IAICentralEndpointSelectorBuilder, IEndpointSelector>? _builtEndpointSelectors;

    private bool _servicesAdded;

    public AICentralPipelineAssembler(
        Func<string, HeaderMatchRouter> routeBuilder,
        Dictionary<string, IAICentralClientAuthBuilder> authProviders,
        Dictionary<string, IAICentralEndpointDispatcherBuilder> endpoints,
        Dictionary<string, IAICentralEndpointSelectorBuilder> endpointSelectors,
        Dictionary<string, IAICentralPipelineStepBuilder<IAICentralPipelineStep>> genericSteps,
        ConfigurationTypes.AICentralPipelineConfig[] pipelines)
    {
        _routeBuilder = routeBuilder;
        _authProviders = authProviders;
        _endpoints = endpoints;
        _endpointSelectors = endpointSelectors;
        _genericSteps = genericSteps;
        _pipelines = pipelines;
    }

    public AICentralPipelines AddServices(IServiceCollection services, ILogger startupLogger)
    {
        _servicesAdded = _servicesAdded ? throw new InvalidOperationException("AICentral is already built") : true;

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

        services.AddSingleton<IncomingCallDetector>();
        services.AddSingleton<IAIServiceDetector, AzureOpenAIServiceDetector>();
        services.AddSingleton<IAIServiceDetector, OpenAIServiceDetector>();

        return pipelines;
    }


    private static TRuntimeType GetMiddlewareOrNoOp<TType, TRuntimeType>(
        string? pipelineConfigName,
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
                $"Can not satisfy request for middleware {providerName} in pipeline {pipelineConfigName ?? "<unknown>"}. Did you forget to configure it?");
    }

    private static IEndpointSelector GetEndpointSelector(
        string? pipelineConfigName,
        Dictionary<string, IAICentralEndpointSelectorBuilder> providers,
        Dictionary<IAICentralEndpointSelectorBuilder, IEndpointSelector> runtime,
        string? providerName,
        IEndpointSelector? fallback)
    {
        if (string.IsNullOrEmpty(providerName))
            return fallback ?? throw new ArgumentException($"Can not find pipeline step {providerName}");

        return providers.TryGetValue(providerName, out var provider)
            ? runtime[provider]
            : throw new ArgumentException(
                $"Can not satisfy request for Endpoint Selector {providerName} in pipeline {pipelineConfigName ?? "<unknown>"}. Did you forget to configure it?");
    }

    private AICentralPipelines BuildPipelines(ILogger startupLogger)
    {
        if (!_servicesAdded)
            throw new InvalidOperationException(
                "You must call AddServices on the Assembler before calling BuildPipelines.");

        return new AICentralPipelines(
            _pipelines
                .Select(pipelineConfig =>
                {
                    var pipelineName =
                        Guard.NotNullOrEmptyOrWhitespace(pipelineConfig.Name, nameof(pipelineConfig.Name));

                    var pipelineSteps = pipelineConfig.Steps ?? Array.Empty<string>();

                    var routeBuilder =
                        _routeBuilder(
                            Guard.NotNullOrEmptyOrWhitespace(pipelineConfig.Host, nameof(pipelineConfig.Host)));
                    
                    startupLogger.LogInformation("Configuring Pipeline {Name} on Host {Host}", pipelineConfig.Name,
                        pipelineConfig.Host);

                    var pipeline = new AICentralPipeline(
                        pipelineName,
                        routeBuilder,
                        GetMiddlewareOrNoOp(
                            pipelineConfig.Name,
                            _authProviders,
                            _builtAuthProviders!,
                            pipelineConfig.AuthProvider,
                            new AllowAnonymousClientAuthProvider()),
                        pipelineSteps.Select(step => GetMiddlewareOrNoOp(
                            pipelineConfig.Name,
                            _genericSteps,
                            _builtSteps!,
                            step,
                            default)).ToArray(),
                        GetEndpointSelector(
                            pipelineConfig.Name,
                            _endpointSelectors,
                            _builtEndpointSelectors!,
                            pipelineConfig.EndpointSelector,
                            default));

                    startupLogger.LogInformation("Configured Pipeline {Name} on Host {Host}", pipelineConfig.Name,
                        pipelineConfig.Host);

                    return pipeline;
                }).ToArray());
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
            otherAssembler._pipelines.Union(_pipelines).ToArray()
        );
    }
}