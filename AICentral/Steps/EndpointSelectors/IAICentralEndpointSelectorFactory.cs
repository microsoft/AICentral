using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors;

public interface IAICentralEndpointSelectorFactory
{
    static virtual string ConfigName  => throw new NotImplementedException();

    static virtual IAICentralEndpointSelectorFactory BuildFromConfig(
        ILogger logger, 
        IConfigurationSection section,
        Dictionary<string, IAICentralEndpointDispatcherFactory> aiCentralEndpoints) => throw new NotImplementedException();

    IEndpointSelector Build();

    void RegisterServices(IServiceCollection services);

    object WriteDebug();
}