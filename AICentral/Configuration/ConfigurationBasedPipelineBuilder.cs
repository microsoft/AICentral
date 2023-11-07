using System.Reflection;
using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Auth;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.EndpointSelectors;
using AICentral.PipelineComponents.Routes;

namespace AICentral.Configuration;

public class ConfigurationBasedPipelineBuilder
{
    private readonly Dictionary<string, Func<IConfigurationSection, IAICentralEndpointDispatcherBuilder>>
        _endpointConfigurationBuilders = new();

    private readonly
        Dictionary<string, Func<IConfigurationSection, Dictionary<string, IAICentralEndpointDispatcherBuilder>,
            IAICentralEndpointSelectorBuilder>> _endpointSelectorConfigurations = new();

    private readonly Dictionary<string,
            Func<IConfigurationSection, IAICentralPipelineStepBuilder<IAICentralPipelineStep>>>
        _genericStepBuilders = new();

    private readonly
        Dictionary<string, Func<IConfigurationSection, IAICentralClientAuthBuilder>>
        _authProviderBuilders = new();

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
        IConfigurationSection configurationSection,
        params Assembly[] additionalAssembliesToScan)
    {
        RegisterBuilders<IAICentralEndpointSelectorBuilder>(additionalAssembliesToScan,
            nameof(RegisterEndpointSelector));
        RegisterBuilders<IAICentralEndpointDispatcherBuilder>(additionalAssembliesToScan, nameof(RegisterEndpoint));
        RegisterBuilders<IAICentralGenericStepBuilder<IAICentralPipelineStep>>(additionalAssembliesToScan,
            nameof(RegisterGenericStep));
        RegisterBuilders<IAICentralClientAuthBuilder>(additionalAssembliesToScan, nameof(RegisterAuthProvider));
        
        var endpoints =
            configurationSection
                .GetSection("Endpoints")
                .GetChildren()
                .Select(x => new
                {
                    TypeInfo = x.Get<ConfigurationTypes.AICentralTypeAndNameConfig>(),
                    Config = x
                })
                .ToDictionary(
                    x => Guard.NotNull(x.TypeInfo?.Name, x.Config, "Name"),
                    x =>
                    {
                        startupLogger.LogInformation("Configuring Endpoint {Name}", x.TypeInfo!.Name);
                        return _endpointConfigurationBuilders[
                            Guard.NotNull(x.TypeInfo?.Type, x.Config, "Type") ??
                            throw new ArgumentException("No Type specified for Endpoint")](
                            x.Config);
                    });

        var endpointSelectors =
            configurationSection
                .GetSection("EndpointSelectors")
                .GetChildren()
                .Select(x => new
                {
                    TypeInfo = x.Get<ConfigurationTypes.AICentralTypeAndNameConfig>(),
                    Config = x
                })
                .ToDictionary(
                    x => Guard.NotNull(x.TypeInfo?.Name, x.Config, "Name"),
                    x =>
                    {
                        startupLogger.LogInformation("Configuring Endpoint Selector {Name}", x.TypeInfo!.Name);
                        return _endpointSelectorConfigurations[
                            Guard.NotNull(x.TypeInfo?.Type, x.Config, "Type") ??
                            throw new ArgumentException("No Type specified for Endpoint")](x.Config, endpoints);
                    });

        var authProviders =
            configurationSection
                .GetSection("AuthProviders")
                .GetChildren()
                .Select(x => new
                {
                    TypeInfo = x.Get<ConfigurationTypes.AICentralTypeAndNameConfig>(),
                    Config = x
                })
                .ToDictionary(
                    x => Guard.NotNull(x.TypeInfo?.Name, x.Config, "Name"),
                    x =>
                    {
                        startupLogger.LogInformation("Configuring AuthProviders {Name}", x.TypeInfo!.Name);
                        return _authProviderBuilders[
                            Guard.NotNull(x.TypeInfo?.Type, x.Config, "Type") ??
                            throw new ArgumentException("No Type specified for Endpoint")](x.Config);
                    });

        var genericSteps =
            configurationSection
                .GetSection("GenericSteps")
                .GetChildren()
                .Select(x => new
                {
                    TypeInfo = x.Get<ConfigurationTypes.AICentralTypeAndNameConfig>(),
                    Config = x
                })
                .ToDictionary(
                    x => Guard.NotNull(x.TypeInfo?.Name, x.Config, "Name"),
                    x =>
                    {
                        startupLogger.LogInformation("Configuring AuthProviders {Name}", x.TypeInfo!.Name);
                        return _genericStepBuilders[
                            Guard.NotNull(x.TypeInfo?.Type, x.Config, "Type") ??
                            throw new ArgumentException("No Type specified for Endpoint")](x.Config);
                    });

        var typedConfig = configurationSection
            .Get<ConfigurationTypes.AICentralConfig>();

        //create an object that can wire all this together
        var builder = new AICentralPipelineAssembler(
            PathMatchRouter.WithPath,
            authProviders,
            endpoints,
            endpointSelectors,
            genericSteps,
            typedConfig?.Pipelines ?? Array.Empty<ConfigurationTypes.AICentralPipelineConfig>()
        );

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
                    .Where(x1 => x1 is { IsInterface: false, IsAbstract: false })
                    .Where(x1 => x1.IsAssignableTo(typeof(T))))
            .ToArray();
        return testEndpointSelectors;
    }
}