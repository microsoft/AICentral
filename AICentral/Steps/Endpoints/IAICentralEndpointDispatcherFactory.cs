using AICentral.Core;

namespace AICentral.Steps.Endpoints;

public interface IAICentralEndpointDispatcherFactory
{
    static virtual string ConfigName  => throw new NotImplementedException();

    void RegisterServices(AICentralOptions options, IServiceCollection services);

    IAICentralEndpointDispatcher Build();

    static virtual IAICentralEndpointDispatcherFactory BuildFromConfig(ILogger logger, IConfigurationSection configurationSection)
    {
        throw new NotImplementedException();
    }

    object WriteDebug();
}