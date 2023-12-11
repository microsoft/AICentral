namespace AICentral.Core;

public interface IAICentralEndpointSelectorFactory
{
    static virtual string ConfigName  => throw new NotImplementedException();

    static virtual IAICentralEndpointSelectorFactory BuildFromConfig(
        ILogger logger, 
        AICentralTypeAndNameConfig config,
        Dictionary<string, IAICentralEndpointDispatcherFactory> aiCentralEndpoints
        ) => throw new NotImplementedException();

    IAICentralEndpointSelector Build();

    void RegisterServices(IServiceCollection services);

    object WriteDebug();
}