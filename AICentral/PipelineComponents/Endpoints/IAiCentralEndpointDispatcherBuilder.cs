using AICentral.Configuration.JSON;

namespace AICentral.PipelineComponents.Endpoints;

public interface IAICentralEndpointDispatcherBuilder: IAICentralPipelineStepBuilder<IAICentralEndpointDispatcher>
{
    static virtual IAICentralEndpointDispatcherBuilder BuildFromConfig(ConfigurationTypes.AICentralPipelineEndpointPropertiesConfig parameters)
    {
        throw new NotImplementedException();
    }
}