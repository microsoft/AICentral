using System.Reflection;
using AICentral.Core;
using AICentral.Endpoints;

namespace AICentral.Configuration;

/// <summary>
/// Assists with construction of Pipeline Assemblers which build pipelines.
/// </summary>
/// <remarks>
/// This class makes it easy to build Pipeline Assemblers using Asp.Net Core's Configuration system. 
/// </remarks>
public class ConfigurationBasedPipelineBuilder
{
    private readonly Dictionary<string,
            Func<ILogger, TypeAndNameConfig, IDictionary<string, IEndpointAuthorisationHandlerFactory>, IDownstreamEndpointAdapterFactory>>
        _endpointConfigurationBuilders = new();

    private readonly
        Dictionary<string, Func<ILogger, TypeAndNameConfig,
            Dictionary<string, IEndpointDispatcherFactory>,
            IEndpointSelectorFactory>> _endpointSelectorConfigurations = new();

    private readonly Dictionary<string,
            Func<ILogger, TypeAndNameConfig, IEndpointAuthorisationHandlerFactory>>
        _backendAuths = new();

    private readonly Dictionary<string,
            Func<ILogger, TypeAndNameConfig, IRouteProxy>>
        _routeProxies = new();

    private readonly Dictionary<string,
            Func<ILogger, TypeAndNameConfig, IPipelineStepFactory>>
        _genericStepBuilders = new();

    private readonly
        Dictionary<string, Func<ILogger, TypeAndNameConfig, IPipelineStepFactory>>
        _authProviderBuilders = new();

    private void RegisterAuthProvider<T>() where T : IPipelineStepFactory =>
        _authProviderBuilders.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterEndpoint<T>() where T : IDownstreamEndpointAdapterFactory =>
        _endpointConfigurationBuilders.Add(T.ConfigName, T.BuildFromConfig);

    // ReSharper disable once UnusedMember.Local
    private void RegisterEndpointSelector<T>() where T : IEndpointSelectorFactory =>
        _endpointSelectorConfigurations.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterGenericStep<T>() where T : IPipelineStepFactory =>
        _genericStepBuilders.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterBackendEndpointAuthoriser<T>() where T : IEndpointAuthorisationHandlerFactory =>
        _backendAuths.Add(T.ConfigName, T.BuildFromConfig);

    private void RegisterRouteProxy<T>() where T : IRouteProxy =>
        _routeProxies.Add(T.ConfigName, T.BuildFromConfig);

    public AICentralPipelineAssembler BuildPipelinesFromConfig(
        AICentralConfig configuration,
        ILogger startupLogger,
        params Assembly[] additionalAssembliesToScan)
    {
        RegisterBuilders<IEndpointSelectorFactory>(additionalAssembliesToScan, nameof(RegisterEndpointSelector));
        RegisterBuilders<IDownstreamEndpointAdapterFactory>(additionalAssembliesToScan, nameof(RegisterEndpoint));
        RegisterBuilders<IPipelineStepFactory>(additionalAssembliesToScan, nameof(RegisterGenericStep));
        RegisterBuilders<IPipelineStepFactory>(additionalAssembliesToScan, nameof(RegisterAuthProvider));
        RegisterBuilders<IEndpointAuthorisationHandlerFactory>(additionalAssembliesToScan, nameof(RegisterBackendEndpointAuthoriser));
        RegisterBuilders<IRouteProxy>(additionalAssembliesToScan, nameof(RegisterRouteProxy));
        
        var routeProxies =
            (configuration
                .RouteProxies ?? [])
            .ToDictionary(
                x => Guard.NotNull(x.Name, "Name"),
                x =>
                {
                    startupLogger.LogInformation("Configuring Route Proxy {Name}", x.Name);
                    return _routeProxies[
                        Guard.NotNull(x.Type, "Type") ??
                        throw new ArgumentException("No Type specified for Route Proxy")](
                        startupLogger,
                        x
                    );
                });        
        
        var backendAuths =
            (configuration
                .BackendAuths ?? [])
                .ToDictionary(
                    x => Guard.NotNull(x.Name, "Name"),
                    x =>
                    {
                        startupLogger.LogInformation("Configuring Backend Authoriser {Name}", x.Name);
                        return _backendAuths[
                            Guard.NotNull(x.Type, "Type") ??
                            throw new ArgumentException("No Type specified for Backend Authoriser")](
                            startupLogger,
                            x
                        );
                    });

        var endpoints =
            configuration
                .Endpoints!
                .ToDictionary(
                    x => Guard.NotNull(x.Name, "Name"),
                    x =>
                    {
                        startupLogger.LogInformation("Configuring Endpoint {Name}", x.Name);
                        return (IEndpointDispatcherFactory)new DownstreamEndpointDispatcherFactory(
                            _endpointConfigurationBuilders[
                                Guard.NotNull(x.Type, "Type") ??
                                throw new ArgumentException("No Type specified for Endpoint")](
                                startupLogger,
                                x,
                                backendAuths));
                    });

        var endpointSelectors = new Dictionary<string, IEndpointSelectorFactory>();
        foreach (var x in configuration.EndpointSelectors!)
        {
            Guard.NotNull(x.Name, "Name");
            startupLogger.LogInformation("Configuring Endpoint Selector {Name}", x.Name);
            var aiCentralEndpointSelectorFactory = _endpointSelectorConfigurations[
                Guard.NotNull(x.Type, "Type") ??
                throw new ArgumentException("No Type specified for Endpoint Selector")](
                startupLogger,
                x,
                endpoints);

            endpointSelectors.Add(x.Name!, aiCentralEndpointSelectorFactory);
            if (endpoints.ContainsKey(x.Name!))
            {
                startupLogger.LogWarning(
                    "Unable to use Endpoint Selector {Name} as a virtual endpoint within another Endpoint Selector, as it already exists as an Endpoint",
                    x.Name!);
            }
            else
            {
                endpoints.Add(x.Name!, new EndpointSelectorAdapterDispatcherFactory(aiCentralEndpointSelectorFactory));
            }
        }

        var authProviders =
            configuration
                .AuthProviders!
                .ToDictionary(
                    x => Guard.NotNull(x.Name, "Name"),
                    x =>
                    {
                        startupLogger.LogInformation("Configuring AuthProviders {Name}", x.Name);
                        return _authProviderBuilders[
                            Guard.NotNull(x.Type, "Type") ??
                            throw new ArgumentException("No Type specified for AuthProvider")](
                            startupLogger,
                            x
                        );
                    });

        var genericSteps =
            configuration
                .GenericSteps!
                .ToDictionary(
                    x => Guard.NotNull(x.Name, "Name"),
                    x =>
                    {
                        startupLogger.LogInformation("Configuring Generic Step {Name}", x.Name);
                        return _genericStepBuilders[
                            Guard.NotNull(x.Type, "Type") ??
                            throw new ArgumentException("No Type specified for Generic Step")](
                            startupLogger,
                            x
                        );
                    });
        
        //create an object that can wire all this together
        var builder = new AICentralPipelineAssembler(
            HostNameMatchRouter.WithHostHeader,
            authProviders,
            endpoints,
            endpointSelectors,
            genericSteps,
            routeProxies,
            configuration.Pipelines ?? []
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