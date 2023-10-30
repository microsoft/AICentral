using AICentral.Pipelines.Endpoints;

namespace AICentral.Pipelines.EndpointSelectors;

public interface IAICentralEndpointSelector: IAICentralPipelineStep<IAICentralEndpointSelectorRuntime>
{
    static virtual IAICentralEndpointSelector BuildFromConfig(
        Dictionary<string, string> parameters,
        Dictionary<string, IAICentralEndpoint> aiCentralEndpoints) => throw new NotImplementedException();

    object WriteDebug();
}

public interface IAICentralEndpointSelectorRuntime : IAICentralPipelineStepRuntime
{
    
}
