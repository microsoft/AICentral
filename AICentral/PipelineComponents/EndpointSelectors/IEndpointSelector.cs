using AICentral.PipelineComponents.Endpoints;

namespace AICentral.PipelineComponents.EndpointSelectors;

public interface IEndpointSelector
{
    object WriteDebug();

    Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        AICentralPipelineExecutor pipeline, CancellationToken cancellationToken);
}