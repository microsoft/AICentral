namespace AICentral.Core;

public interface IAICentralEndpointDispatcherFactory
{
    static virtual string ConfigName  => throw new NotImplementedException();

    void RegisterServices(
        HttpMessageHandler? httpMessageHandler, 
        IServiceCollection services);

    IAICentralEndpointDispatcher Build();

    static virtual IAICentralEndpointDispatcherFactory BuildFromConfig(ILogger logger, AICentralTypeAndNameConfig config)
    {
        throw new NotImplementedException();
    }

    object WriteDebug();
}