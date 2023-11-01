namespace AICentral.PipelineComponents.EndpointSelectors;

public interface IEndpointSelector
{
    object WriteDebug();

    Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken);
}