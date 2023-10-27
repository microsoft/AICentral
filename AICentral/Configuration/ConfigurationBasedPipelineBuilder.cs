using AICentral.Configuration.JSON;
using AICentral.Pipelines;
using AICentral.Pipelines.Auth;
using AICentral.Pipelines.Endpoints;
using AICentral.Pipelines.Endpoints.AzureOpenAI;
using AICentral.Pipelines.EndpointSelectors;
using AICentral.Pipelines.EndpointSelectors.Random;
using AICentral.Pipelines.EndpointSelectors.Single;
using AICentral.Pipelines.Logging;
using AICentral.Pipelines.RateLimiting;
using AICentral.Pipelines.Routes;

namespace AICentral.Configuration;

public class ConfigurationBasedPipelineBuilder
{
    private static readonly Dictionary<string, Func<Dictionary<string, string>, IAICentralRouter>>
        Routers = new();

    private static readonly Dictionary<string, Func<Dictionary<string, string>, IAICentralEndpoint>>
        EndpointConfigurations = new();

    private static readonly
        Dictionary<string, Func<Dictionary<string, string>, Dictionary<string, IAICentralEndpoint>,
            IAICentralEndpointSelector>> EndpointSelectorConfigurations = new();

    private static readonly Dictionary<string, Func<Dictionary<string, string>, IAICentralPipelineStep>>
        PipelineConfigurations = new();

    private static readonly
        Dictionary<string, Func<IConfigurationSection, Dictionary<string, string>, IAICentralClientAuthProvider>>
        AuthProviders = new();

    private static readonly
        Dictionary<string, Func<IConfigurationSection, Dictionary<string, string>, IAICentralRateLimitingProvider>>
        RateLimitingProviders = new();

    private void RegisterRouter<T>() where T : IAICentralRouter =>
        Routers.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterAuthProvider<T>() where T : IAICentralClientAuthProvider =>
        AuthProviders.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterRateLimitingProvider<T>() where T : IAICentralRateLimitingProvider =>
        RateLimitingProviders.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterEndpoint<T>() where T : IAICentralEndpoint =>
        EndpointConfigurations.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterEndpointSelector<T>() where T : IAICentralEndpointSelector =>
        EndpointSelectorConfigurations.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterPipelineStep<T>() where T : IAICentralPipelineStep =>
        PipelineConfigurations.Add(T.ConfigName, T.BuildFromConfig);

    public AICentralOptions BuildPipelinesFromConfig(ILogger startupLogger, IConfigurationSection configurationSection, ConfigurationTypes.AICentralConfig configSection)
    {
        RegisterEndpointSelector<RandomEndpointSelector>();
        RegisterEndpointSelector<SingleEndpointSelector>();
        RegisterEndpoint<AzureOpenAIEndpoint>();
        RegisterPipelineStep<AzureMonitorLoggerPipelineStep>();
        RegisterAuthProvider<NoClientAuthAuthProvider>();
        RegisterAuthProvider<EntraAuthProviderProvider>();
        RegisterRateLimitingProvider<RateLimitingProvider>();
        RegisterRateLimitingProvider<NoRateLimitingProvider>();
        RegisterRouter<SimplePathMatchRouter>();
        
        var configEndpoints =
            configSection.Endpoints ?? Array.Empty<ConfigurationTypes.AICentralPipelineEndpointConfig>();

        var configEndpointSelectors = configSection.EndpointSelectors ??
                                      Array.Empty<ConfigurationTypes.AICentralPipelineEndpointSelectorConfig>();

        var endpoints = configEndpoints.ToDictionary(
            x => x.Name ?? throw new ArgumentException("Missing Name for Endpoint"), x =>
            {
                startupLogger.LogInformation("Configuring Endpoint {Name}", x.Name);
                return EndpointConfigurations[x.Type ?? throw new ArgumentException("No Type specified for Endpoint")](
                    x.Properties ?? throw new ArgumentException("No Properties specified for Endpoint"));
            });
        var endpointSelectors = configEndpointSelectors.ToDictionary(
            x => x.Name ?? throw new ArgumentException("Missing Name for Endpoint"), x =>
            {
                startupLogger.LogInformation("Configuring Endpoint Selector {Name}", x.Name);
                return EndpointSelectorConfigurations
                    [x.Type ?? throw new ArgumentException("No Type specified for Endpoint Selector")](
                    x.Properties ?? throw new ArgumentException("No Properties specified for Endpoint Selector"),
                    endpoints);
            });
        var authProviders = (configSection.AuthProviders ?? Array.Empty<ConfigurationTypes.AICentralAuthConfig>())
            .ToDictionary(x => x.Name ?? throw new ArgumentException("No Name specified for Auth Provider"), x =>
            {
                startupLogger.LogInformation("Configuring Auth Provider {Name}", x.Name);
                return AuthProviders[x.Type ?? throw new ArgumentException("No Type specified for Auth Provider")](
                    configurationSection.GetSection("AuthProviders:0"),
                    x.Properties ?? throw new ArgumentException("No Properties specified for Auth Provider"));
            });
        var rateLimiters =
            (configSection.RateLimitingProviders ?? Array.Empty<ConfigurationTypes.AICentralRateLimitingConfig>()).ToDictionary(
                x => x.Name ?? throw new ArgumentException("No Name specified for Rate Limiter"), x =>
                {
                    startupLogger.LogInformation("Configuring Rate Limiter {Name}", x.Name);
                    return RateLimitingProviders
                        [x.Type ?? throw new ArgumentException("No Type specified for Rate Limiter")](
                        configurationSection.GetSection("RateLimitingProviders:0"),
                        x.Properties ?? throw new ArgumentException("No Properties specified for Rate Limimter"));
                });

        var configPipelines =
            configSection.Pipelines ?? Array.Empty<ConfigurationTypes.AICentralPipelineConfig>();

        var pipelines = configPipelines.Select(x =>
        {
            startupLogger.LogInformation("Configuring Pipeline {Name} listening on Route {Route}", x.Name, x.Path);

            TType GetMiddlewareOrNoOp<TType>(Dictionary<string, TType> providers, string? providerName, TType fallback)
                where TType : IAICentralAspNetCoreMiddlewarePlugin
            {
                if (string.IsNullOrEmpty(providerName))
                    return fallback;
                
                return providers.TryGetValue(providerName, out var provider)
                    ? provider
                    : throw new ArgumentException(
                        $"Can not satisfy request for middleware {providerName}. Did you forget to configure it?");
            }

            var pipelineName = string.IsNullOrEmpty(x.Name)
                ? throw new ArgumentException("Missing Name for pipeline")
                : x.Name!;
            var pipelineSteps = x.Steps ??
                                throw new ArgumentException(
                                    $"No Pipelines steps specified in config for Pipeline {pipelineName}");
            if (configPipelines.Length == 0) throw new ArgumentException("No Pipelines specified in config");

            return new AICentralPipeline(
                pipelineName,
                Routers[(x.Path?.Type ?? throw new ArgumentException("Missing Path for pipeline"))](x.Path.Properties ?? throw new ArgumentException("Missing properties for path")),
                GetMiddlewareOrNoOp(rateLimiters, x.RateLimiter, new NoRateLimitingProvider()),
                GetMiddlewareOrNoOp(authProviders, x.AuthProvider, new NoClientAuthAuthProvider()),

                pipelineSteps.Select(step =>
                {
                    startupLogger.LogInformation(" > Configuring Step {Name}", step.Type);
                    return PipelineConfigurations[
                        step.Type ??
                        throw new ArgumentException($"Missing Type property in Step for Pipeline {pipelineName}")](
                        step.Properties ??
                        throw new ArgumentException($"Missing Properties in Step for Pipeline {pipelineName}"));
                }).ToArray(),
                endpointSelectors[
                    x.EndpointSelector ??
                    throw new ArgumentException($"Missing EndpointSelector for Pipeline {x.EndpointSelector}")]
            );
        }).ToArray();

        startupLogger.LogInformation("AI Central has configured {Count} pipelines", pipelines.Length);

        return new AICentralOptions()
        {
            Pipelines = pipelines
        };
    }
}