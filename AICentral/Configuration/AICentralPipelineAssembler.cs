using AICentral.Auth;
using AICentral.Core;
using AICentral.Routes;

namespace AICentral.Configuration;

/// <summary>
/// Responsible for assembling the pipelines from all the factories.
/// This class will also Add all the required services needed for the pipelines.
/// </summary>
public class AICentralPipelineAssembler
{
    private readonly Func<string, HeaderMatchRouter> _routeBuilder;
    private readonly Dictionary<string, IAICentralClientAuthFactory> _authProviders;
    private readonly Dictionary<string, IEndpointRequestResponseHandlerFactory> _endpoints;
    private readonly Dictionary<string, IAICentralEndpointSelectorFactory> _endpointSelectors;
    private readonly Dictionary<string, IAICentralGenericStepFactory> _genericSteps;
    private readonly AICentralPipelineConfig[] _pipelines;

    private bool _servicesAdded;

    public AICentralPipelineAssembler(
        Func<string, HeaderMatchRouter> routeBuilder,
        Dictionary<string, IAICentralClientAuthFactory> authProviders,
        Dictionary<string, IEndpointRequestResponseHandlerFactory> endpoints,
        Dictionary<string, IAICentralEndpointSelectorFactory> endpointSelectors,
        Dictionary<string, IAICentralGenericStepFactory> genericSteps,
        AICentralPipelineConfig[] pipelines)
    {
        _routeBuilder = routeBuilder;
        _authProviders = authProviders;
        _endpoints = endpoints;
        _endpointSelectors = endpointSelectors;
        _genericSteps = genericSteps;
        _pipelines = pipelines;
    }

    public AICentralPipelines AddServices(
        IServiceCollection services,
        HttpMessageHandler? optionalHandler,
        ILogger startupLogger)
    {
        _servicesAdded = _servicesAdded ? throw new InvalidOperationException("AICentral is already built") : true;

        services.AddSingleton<DateTimeProvider>();
        services.AddSingleton<InMemoryRateLimitingTracker>();

        foreach (var authProvider in _authProviders) authProvider.Value.RegisterServices(services);
        foreach (var endpoint in _endpoints) endpoint.Value.RegisterServices(optionalHandler, services);
        foreach (var endpointSelector in _endpointSelectors) endpointSelector.Value.RegisterServices(services);
        foreach (var step in _genericSteps) step.Value.RegisterServices(services);

        var pipelines = BuildPipelines(startupLogger);
        services.AddSingleton(pipelines);

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return pipelines;
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
                        _authProviders.ContainsKey(pipelineConfig.AuthProvider ??
                                                   throw new ArgumentException(
                                                       $"No AuthProvider for pipeline {pipelineConfig.Name}"))
                            ? _authProviders[pipelineConfig.AuthProvider ?? string.Empty]
                            : throw new ArgumentException($"Cannot find Auth Provider {pipelineConfig.AuthProvider}"),
                        pipelineSteps.Select(step =>
                            _genericSteps.ContainsKey(step)
                                ? _genericSteps[step ?? string.Empty]
                                : throw new ArgumentException($"Cannot find Step {step}")).ToArray(),
                        _endpointSelectors.ContainsKey(
                            pipelineConfig.EndpointSelector ??
                            throw new ArgumentException($"No EndpointSelector for pipeline {pipelineConfig.Name}"))
                            ? _endpointSelectors[pipelineConfig.EndpointSelector ?? string.Empty]
                            : throw new ArgumentException(
                                $"Cannot find EndpointSelector {pipelineConfig.EndpointSelector}"));

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
