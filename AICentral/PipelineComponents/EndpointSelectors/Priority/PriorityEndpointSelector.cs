using AICentral.PipelineComponents.EndpointSelectors.Random;

namespace AICentral.PipelineComponents.EndpointSelectors.Priority;

public class PriorityEndpointSelector : IEndpointSelector
{
    private readonly RandomEndpointSelector _prioritisedOpenAiEndpointSelector;
    private readonly RandomEndpointSelector _fallbackOpenAiEndpointSelector;

    public PriorityEndpointSelector(
        RandomEndpointSelector prioritisedOpenAiEndpointSelector,
        RandomEndpointSelector fallbackOpenAiEndpointSelector)
    {
        _prioritisedOpenAiEndpointSelector = prioritisedOpenAiEndpointSelector;
        _fallbackOpenAiEndpointSelector = fallbackOpenAiEndpointSelector;
    }

    public async Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<RandomEndpointSelectorBuilder>>();
        try
        {
            logger.LogDebug("Prioritised Endpoint selector handling request");
            return await _prioritisedOpenAiEndpointSelector.Handle(context, pipeline, cancellationToken);
        }
        catch (Exception)
        {
            try
            {
                logger.LogWarning("e, Prioritised Endpoint selector failed with primary. Trying fallback servers");
                return await _fallbackOpenAiEndpointSelector.Handle(context, pipeline, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to handle request. Exhausted endpoints");
                throw new InvalidOperationException("No available Open AI hosts", e);
            }
        }
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "Priority Router",
            PrioritisedEndpoints = _prioritisedOpenAiEndpointSelector.WriteDebug(),
            FallbackEndpoints = _fallbackOpenAiEndpointSelector.WriteDebug(),
        };
    }
}