namespace AICentral.Pipelines.Endpoints;

public interface IAICentralEndpoint: IAICentralPipelineStep<IAICentralEndpointRuntime>
{
    static virtual IAICentralEndpoint BuildFromConfig(Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }
}


public interface IAICentralEndpointRuntime : IAICentralPipelineStepRuntime
{
}