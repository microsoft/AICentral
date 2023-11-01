using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Auth;
using AICentral.PipelineComponents.Auth.AllowAnonymous;
using AICentral.PipelineComponents.Auth.Entra;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.AzureOpenAI;
using AICentral.PipelineComponents.EndpointSelectors;
using AICentral.PipelineComponents.EndpointSelectors.Random;
using AICentral.PipelineComponents.EndpointSelectors.Single;
using AICentral.PipelineComponents.Logging;
using AICentral.PipelineComponents.RateLimiting;
using AICentral.PipelineComponents.Routes;

namespace AICentral.Configuration;

public class ConfigurationBasedPipelineBuilder
{
    private static readonly Dictionary<string, Func<Dictionary<string, string>, IAICentralRouter>>
        Routers = new();

    private static readonly Dictionary<string, Func<ConfigurationTypes.AICentralPipelineEndpointPropertiesConfig, IAICentralEndpointDispatcherBuilder>>
        EndpointConfigurations = new();

    private static readonly
        Dictionary<string, Func<Dictionary<string, string>, Dictionary<string, IAICentralEndpointDispatcherBuilder>,
            IAICentralEndpointSelectorBuilder>> EndpointSelectorConfigurations = new();

    private static readonly Dictionary<string, Func<Dictionary<string, string>, IAICentralPipelineStepBuilder<IAICentralPipelineStep>>>
        GenericSteps = new();

    private static readonly
        Dictionary<string, Func<IConfigurationSection, Dictionary<string, string>, IAICentralClientAuthBuilder>>
        AuthProviders = new();

    // private static readonly
    //     Dictionary<string, Func<IConfigurationSection, Dictionary<string, string>, IAICentralRateLimitingProvider>>
    //     RateLimitingProviders = new();

    private void RegisterRouter<T>() where T : IAICentralRouter =>
        Routers.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterAuthProvider<T>() where T : IAICentralClientAuthBuilder =>
        AuthProviders.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterEndpoint<T>() where T : IAICentralEndpointDispatcherBuilder =>
        EndpointConfigurations.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterEndpointSelector<T>() where T : IAICentralEndpointSelectorBuilder =>
        EndpointSelectorConfigurations.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterGenericStep<T>() where T : IAICentralGenericStepBuilder<IAICentralPipelineStep> =>
        GenericSteps.Add(T.ConfigName, T.BuildFromConfig);

    public AICentralPipelineAssembler BuildPipelinesFromConfig(ILogger startupLogger, IConfigurationSection configurationSection, ConfigurationTypes.AICentralConfig? configSection)
    {
        RegisterEndpointSelector<RandomEndpointSelectorBuilder>();
        RegisterEndpointSelector<SingleEndpointSelectorBuilder>();
        RegisterEndpoint<AzureOpenAIEndpointDispatcherBuilder>();
        RegisterGenericStep<AzureMonitorLoggerBuilder>();
        RegisterAuthProvider<AllowAnonymousClientAuthBuilder>();
        RegisterAuthProvider<EntraClientAuthBuilder>();
        RegisterGenericStep<RateLimitingProvider>();
        RegisterGenericStep<NoRateLimitingProvider>();
        RegisterRouter<SimplePathMatchRouter>();
        
        configSection ??= new ConfigurationTypes.AICentralConfig(); 

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
        
        var genericSteps = (configSection.GenericSteps ?? Array.Empty<ConfigurationTypes.AICentralGenericStepConfig>())
            .ToDictionary(x => x.Name ?? throw new ArgumentException("No Name specified for Generic Step"), x =>
            {
                startupLogger.LogInformation("Configuring Generic Step {Name}", x.Name);
                return GenericSteps[x.Type ?? throw new ArgumentException("No Type specified for Generic Step")](
                    x.Properties ?? throw new ArgumentException("No Properties specified for Generic Step"));
            });

        var configPipelines =
            configSection.Pipelines ?? Array.Empty<ConfigurationTypes.AICentralPipelineConfig>();
        
        //create an object that can wire all this together
        var builder = new AICentralPipelineAssembler(
            Routers,
            authProviders,
            endpoints,
            endpointSelectors,
            genericSteps,
            configPipelines);

        return builder;
    }
}