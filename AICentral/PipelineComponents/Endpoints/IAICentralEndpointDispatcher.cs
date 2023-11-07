namespace AICentral.PipelineComponents.Endpoints;

public interface IAICentralEndpointDispatcher
{
    Task<(AICentralRequestInformation, HttpResponseMessage)> Handle(
        HttpContext context, 
        AICallInformation callInformation,
        AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);

    object WriteDebug();
}