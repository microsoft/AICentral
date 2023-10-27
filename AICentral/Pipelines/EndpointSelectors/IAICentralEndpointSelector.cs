using AICentral.Pipelines.Endpoints;

namespace AICentral.Pipelines.EndpointSelectors;

public interface IAICentralEndpointSelector
{
    static virtual string ConfigName  => throw new NotImplementedException();

    static virtual IAICentralEndpointSelector BuildFromConfig(
        Dictionary<string, string> parameters,
        Dictionary<string, IAICentralEndpoint> aiCentralEndpoints) => throw new NotImplementedException();

    Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);

    object WriteDebug();
}