using System.Reflection;
using AICentral.Configuration.JSON;
using AICentral.Core;
using AICentral.Steps.Auth;
using AICentral.Steps.Endpoints;
using AICentral.Steps.EndpointSelectors;
using AICentral.Steps.Routes;

namespace AICentral.Configuration;

public class ConfigurationBasedPipelineBuilder
{
    private readonly Dictionary<string, Func<ILogger, IConfigurationSection, IAICentralEndpointDispatcherFactory>>
        _endpointConfigurationBuilders = new();

    private readonly
        Dictionary<string, Func<ILogger, IConfigurationSection, Dictionary<string, IAICentralEndpointDispatcherFactory>,
            Dictionary<string, IAICentralEndpointSelectorFactory>,
            IAICentralEndpointSelectorFactory>> _endpointSelectorConfigurations = new();

    private readonly Dictionary<string,
            Func<ILogger, IConfigurationSection, IAICentralGenericStepFactory>>
        _genericStepBuilders = new();

    private readonly
        Dictionary<string, Func<ILogger, IConfigurationSection, IAICentralClientAuthFactory>>
        _authProviderBuilders = new();

    private void RegisterAuthProvider<T>() where T : IAICentralClientAuthFactory =>
        _authProviderBuilders.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterEndpoint<T>() where T : IAICentralEndpointDispatcherFactory =>
        _endpointConfigurationBuilders.Add(T.ConfigName, T.BuildFromConfig);

    // ReSharper disable once UnusedMember.Local
    private void RegisterEndpointSelector<T>() where T : IAICentralEndpointSelectorFactory =>
        _endpointSelectorConfigurations.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterGenericStep<T>() where T : IAICentralGenericStepFactory =>
        _genericStepBuilders.Add(T.ConfigName, T.BuildFromConfig);

    public AICentralPipelineAssembler BuildPipelinesFromConfig(ILogger startupLogger,
        IConfigurationSection configurationSection,
        params Assembly[] additionalAssembliesToScan)
    {
        RegisterBuilders<IAICentralEndpointSelectorFactory>(additionalAssembliesToScan,
            nameof(RegisterEndpointSelector));
        RegisterBuilders<IAICentralEndpointDispatcherFactory>(additionalAssembliesToScan, nameof(RegisterEndpoint));
        RegisterBuilders<IAICentralGenericStepFactory>(additionalAssembliesToScan,
            nameof(RegisterGenericStep));
        RegisterBuilders<IAICentralClientAuthFactory>(additionalAssembliesToScan, nameof(RegisterAuthProvider));

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
                            startupLogger,
                            x.Config);
                    });

        var allEndpointSelectors =
            configurationSection
                .GetSection("EndpointSelectors")
                .GetChildren()
                .Select(x => new
                {
                    TypeInfo = x.Get<ConfigurationTypes.AICentralTypeAndNameConfig>(),
                    Config = x
                });

        var endpointSelectors = new Dictionary<string, IAICentralEndpointSelectorFactory>();
        foreach (var x in allEndpointSelectors)
        {
            Guard.NotNull(x.TypeInfo?.Name, x.Config, "Name");
            startupLogger.LogInformation("Configuring Endpoint Selector {Name}", x.TypeInfo!.Name);
            endpointSelectors.Add(x.TypeInfo.Name!,
                _endpointSelectorConfigurations[
                    Guard.NotNull(x.TypeInfo?.Type, x.Config, "Type") ??
                    throw new ArgumentException("No Type specified for Endpoint")](
                    startupLogger,
                    x.Config,
                    endpoints,
                    endpointSelectors));
        }

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
                            throw new ArgumentException("No Type specified for Endpoint")](
                            startupLogger,
                            x.Config
                        );
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
                            throw new ArgumentException("No Type specified for Endpoint")](
                            startupLogger,
                            x.Config
                        );
                    });

        var typedConfig = configurationSection
            .Get<ConfigurationTypes.AICentralConfig>();

        //create an object that can wire all this together
        var builder = new AICentralPipelineAssembler(
            HeaderMatchRouter.WithHostHeader,
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
        var testEndpointSelectors = AssemblyEx.GetTypesOfType<T>(additionalAssembliesToScan);
        foreach (var selector in testEndpointSelectors)
        {
            typeof(ConfigurationBasedPipelineBuilder)
                .GetMethod(registerMethodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(selector).Invoke(this, Array.Empty<object>());
        }
    }
}