using AICentral.Steps.Endpoints;
using AICentral.Steps.EndpointSelectors.Random;

namespace AICentral.Steps.EndpointSelectors.Priority;

public class PriorityEndpointSelector : EndpointSelectorBase
{
    private readonly System.Random _rnd = new(Environment.TickCount);
    private readonly IAICentralEndpointDispatcher[] _prioritisedOpenAiEndpoints;
    private readonly IAICentralEndpointDispatcher[] _fallbackOpenAiEndpoints;

    public PriorityEndpointSelector(
        IAICentralEndpointDispatcher[] prioritisedOpenAiEndpoints,
        IAICentralEndpointDispatcher[] fallbackOpenAiEndpoints)
    {
        _prioritisedOpenAiEndpoints = prioritisedOpenAiEndpoints;
        _fallbackOpenAiEndpoints = fallbackOpenAiEndpoints;
    }

    public override async Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<RandomEndpointSelectorBuilder>>();
        try
        {
            logger.LogDebug("Prioritised Endpoint selector handling request");
            return await Handle(context, aiCallInformation, pipeline, cancellationToken, _prioritisedOpenAiEndpoints, false);
        }
        catch (HttpRequestException e)
        {
            try
            {
                logger.LogWarning(e, "Prioritised Endpoint selector failed with primary. Trying fallback servers");
                return await Handle(context, aiCallInformation, pipeline, cancellationToken, _fallbackOpenAiEndpoints, true);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to handle request. Exhausted endpoints");
                throw;
            }
        }
    }

    private async Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation aiCallInformation,
        AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken,
        IAICentralEndpointDispatcher[] endpoints,
        bool isFallbackCollection)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<RandomEndpointSelectorBuilder>>();
        var toTry = endpoints.ToList();
        do
        {
            var chosen = toTry.ElementAt(_rnd.Next(0, toTry.Count));
            toTry.Remove(chosen);
            try
            {
                var responseMessage = await chosen.Handle(context, aiCallInformation, cancellationToken); //awaiting to unwrap any Aggregate Exceptions
                
                return await HandleResponse(
                    logger,
                    context,
                    chosen,
                    responseMessage.Item1,
                    responseMessage.Item2,
                    isFallbackCollection && !toTry.Any(),
                    cancellationToken);
            }
            catch (HttpRequestException e)
            {
                if (!toTry.Any())
                {
                    logger.LogError(e, "Failed to handle request. Exhausted endpoints");
                    throw;
                }

                logger.LogWarning(e, "Failed to handle request. Trying another endpoint");
            }
        } while (toTry.Count > 0);

        throw new InvalidOperationException("Failed to satisfy request");
    }

    public override object WriteDebug()
    {
        return new
        {
            Type = "Priority Router",
            PrioritisedEndpoints = _prioritisedOpenAiEndpoints.Select(x => x.WriteDebug()),
            FallbackEndpoints = _fallbackOpenAiEndpoints.Select(x => x.WriteDebug()),
        };
    }
}