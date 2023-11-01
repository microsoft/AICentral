using System.Reflection;
using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Auth;
using AICentral.PipelineComponents.Auth.AllowAnonymous;
using AICentral.PipelineComponents.Auth.Entra;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.OpenAI;
using AICentral.PipelineComponents.EndpointSelectors;
using AICentral.PipelineComponents.EndpointSelectors.Random;
using AICentral.PipelineComponents.EndpointSelectors.Single;
using AICentral.PipelineComponents.Logging;
using AICentral.PipelineComponents.RateLimiting;
using AICentral.PipelineComponents.Routes;

namespace AICentral.Configuration;

public class ConfigurationBasedPipelineBuilder
{
    private readonly Dictionary<string, Func<Dictionary<string, string>, IAICentralRouter>>
        _routerBuilders = new();

    private readonly Dictionary<string, Func<ConfigurationTypes.AICentralPipelineEndpointPropertiesConfig,
            IAICentralEndpointDispatcherBuilder>>
        _endpointConfigurationBuilders = new();

    private readonly
        Dictionary<string, Func<Dictionary<string, string>, Dictionary<string, IAICentralEndpointDispatcherBuilder>,
            IAICentralEndpointSelectorBuilder>> _endpointSelectorConfigurations = new();

    private readonly Dictionary<string,
            Func<Dictionary<string, string>, IAICentralPipelineStepBuilder<IAICentralPipelineStep>>>
        _genericStepBuilders = new();

    private readonly
        Dictionary<string, Func<IConfigurationSection, Dictionary<string, string>, IAICentralClientAuthBuilder>>
        _authProviderBuilders = new();

    private void RegisterRouter<T>() where T : IAICentralRouter =>
        _routerBuilders.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterAuthProvider<T>() where T : IAICentralClientAuthBuilder =>
        _authProviderBuilders.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterEndpoint<T>() where T : IAICentralEndpointDispatcherBuilder =>
        _endpointConfigurationBuilders.Add(T.ConfigName, T.BuildFromConfig);

    // ReSharper disable once UnusedMember.Local
    private void RegisterEndpointSelector<T>() where T : IAICentralEndpointSelectorBuilder =>
        _endpointSelectorConfigurations.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterGenericStep<T>() where T : IAICentralGenericStepBuilder<IAICentralPipelineStep> =>
        _genericStepBuilders.Add(T.ConfigName, T.BuildFromConfig);

    public AICentralPipelineAssembler BuildPipelinesFromConfig(ILogger startupLogger,
        IConfigurationSection configurationSection, ConfigurationTypes.AICentralConfig? configSection,
        params Assembly[] additionalAssembliesToScan)
    {
        RegisterBuilders<IAICentralEndpointSelectorBuilder>(additionalAssembliesToScan, nameof(RegisterEndpointSelector));
        RegisterBuilders<IAICentralEndpointDispatcherBuilder>(additionalAssembliesToScan, nameof(RegisterEndpoint));
        RegisterBuilders<IAICentralGenericStepBuilder<IAICentralPipelineStep>>(additionalAssembliesToScan, nameof(RegisterGenericStep));
        RegisterBuilders<IAICentralClientAuthBuilder>(additionalAssembliesToScan, nameof(RegisterAuthProvider));
        RegisterBuilders<IAICentralRouter>(additionalAssembliesToScan, nameof(RegisterRouter));

        configSection ??= new ConfigurationTypes.AICentralConfig();

        var configEndpoints =
            configSection.Endpoints ?? Array.Empty<ConfigurationTypes.AICentralPipelineEndpointConfig>();

        var configEndpointSelectors = configSection.EndpointSelectors ??
                                      Array.Empty<ConfigurationTypes.AICentralPipelineEndpointSelectorConfig>();

        var endpoints = configEndpoints.ToDictionary(
            x => x.Name ?? throw new ArgumentException("Missing Name for Endpoint"), x =>
            {
                startupLogger.LogInformation("Configuring Endpoint {Name}", x.Name);
                return _endpointConfigurationBuilders[x.Type ?? throw new ArgumentException("No Type specified for Endpoint")](
                    x.Properties ?? throw new ArgumentException("No Properties specified for Endpoint"));
            });

        var endpointSelectors = configEndpointSelectors.ToDictionary(
            x => x.Name ?? throw new ArgumentException("Missing Name for Endpoint"), x =>
            {
                startupLogger.LogInformation("Configuring Endpoint Selector {Name}", x.Name);
                return _endpointSelectorConfigurations
                    [x.Type ?? throw new ArgumentException("No Type specified for Endpoint Selector")](
                    x.Properties ?? throw new ArgumentException("No Properties specified for Endpoint Selector"),
                    endpoints);
            });

        var authProviders = (configSection.AuthProviders ?? Array.Empty<ConfigurationTypes.AICentralAuthConfig>())
            .ToDictionary(x => x.Name ?? throw new ArgumentException("No Name specified for Auth Provider"), x =>
            {
                startupLogger.LogInformation("Configuring Auth Provider {Name}", x.Name);
                return _authProviderBuilders[x.Type ?? throw new ArgumentException("No Type specified for Auth Provider")](
                    configurationSection.GetSection("AuthProviders:0"),
                    x.Properties ?? throw new ArgumentException("No Properties specified for Auth Provider"));
            });

        var genericSteps = (configSection.GenericSteps ?? Array.Empty<ConfigurationTypes.AICentralGenericStepConfig>())
            .ToDictionary(x => x.Name ?? throw new ArgumentException("No Name specified for Generic Step"), x =>
            {
                startupLogger.LogInformation("Configuring Generic Step {Name}", x.Name);
                return _genericStepBuilders[x.Type ?? throw new ArgumentException("No Type specified for Generic Step")](
                    x.Properties ?? throw new ArgumentException("No Properties specified for Generic Step"));
            });

        var configPipelines =
            configSection.Pipelines ?? Array.Empty<ConfigurationTypes.AICentralPipelineConfig>();

        //create an object that can wire all this together
        var builder = new AICentralPipelineAssembler(
            _routerBuilders,
            authProviders,
            endpoints,
            endpointSelectors,
            genericSteps,
            configPipelines);

        return builder;
    }

    private void RegisterBuilders<T>(Assembly[] additionalAssembliesToScan, string registerMethodName)
    {
        var testEndpointSelectors = GetTypesOfType<T>(additionalAssembliesToScan);
        foreach (var selector in testEndpointSelectors)
        {
            typeof(ConfigurationBasedPipelineBuilder)
                .GetMethod(registerMethodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(selector).Invoke(this, Array.Empty<object>());
        }
    }

    private static Type[] GetTypesOfType<T>(Assembly[] additionalAssembliesToScan)
    {
        var testEndpointSelectors = additionalAssembliesToScan
            .Union(new[] { typeof(ConfigurationBasedPipelineBuilder).Assembly }).SelectMany(
                x => x.ExportedTypes
                    .Where(x => x is { IsInterface: false, IsAbstract: false })
                    .Where(x => x.IsAssignableTo(typeof(T))))
            .ToArray();
        return testEndpointSelectors;
    }
}