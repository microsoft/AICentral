using AICentral.Configuration.JSON;

namespace AICentral.PipelineComponents.Endpoints;

public interface IAICentralEndpointDispatcherBuilder
{
    static virtual string ConfigName  => throw new NotImplementedException();

    void RegisterServices(IServiceCollection services);

    IAICentralEndpointDispatcher Build();

    static virtual IAICentralEndpointDispatcherBuilder BuildFromConfig(IConfigurationSection configurationSection)
    {
        throw new NotImplementedException();
    }
}