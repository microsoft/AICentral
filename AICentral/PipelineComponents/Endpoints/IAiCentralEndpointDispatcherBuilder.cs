namespace AICentral.PipelineComponents.Endpoints;

public interface IAiCentralEndpointDispatcherBuilder: IAICentralPipelineStepBuilder<IAICentralEndpointDispatcher>
{
    static virtual IAiCentralEndpointDispatcherBuilder BuildFromConfig(Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }
}