namespace AICentral.Steps.EndpointSelectors;

public interface IEndpointSelector
{
    object WriteDebug();

    Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        AICentralPipelineExecutor pipeline, CancellationToken cancellationToken);
}