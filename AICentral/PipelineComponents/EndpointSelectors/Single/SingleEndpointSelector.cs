using AICentral.PipelineComponents.Endpoints;

namespace AICentral.PipelineComponents.EndpointSelectors.Single;

public class SingleEndpointSelector : EndpointSelectorBase
{
    private readonly IAICentralEndpointDispatcher _endpoint;

    public SingleEndpointSelector(IAICentralEndpointDispatcher endpoint)
    {
        _endpoint = endpoint;
    }

    public override async Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var responseMessage = await _endpoint.Handle(context, aiCallInformation, pipeline, cancellationToken);
        return await HandleResponse(
            context.RequestServices.GetRequiredService<ILogger<SingleEndpointSelector>>(),
            context,
            responseMessage.Item1,
            responseMessage.Item2,
            true,
            cancellationToken
        );
    }

    public override object WriteDebug()
    {
        return new
        {
            Type = "SingleEndpoint",
            Endpoints = new[] { _endpoint.WriteDebug() }
        };
    }
}