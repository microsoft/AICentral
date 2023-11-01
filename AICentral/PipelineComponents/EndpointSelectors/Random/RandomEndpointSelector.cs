using AICentral.PipelineComponents.Endpoints;

namespace AICentral.PipelineComponents.EndpointSelectors.Random;

public class RandomEndpointSelector : IAICentralEndpointSelector
{
    private readonly System.Random _rnd = new(Environment.TickCount);
    private readonly IAICentralEndpointDispatcher[] _openAiServers;

    public RandomEndpointSelector(IAICentralEndpointDispatcher[] openAiServers)
    {
        _openAiServers = openAiServers;
    }

    public async Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<RandomEndpointSelectorBuilder>>();
        var toTry = _openAiServers.ToList();
        logger.LogDebug("Random Endpoint selector is handling request");
        do
        {
            var chosen = toTry.ElementAt(_rnd.Next(0, toTry.Count));
            toTry.Remove(chosen);
            try
            {
                return await chosen.Handle(context, pipeline, cancellationToken); //awaiting to unwrap any Aggregate Exceptions
            }
            catch (Exception e)
            {
                if (!toTry.Any())
                {
                    logger.LogError(e, "Failed to handle request. Exhausted endpoints");
                    throw new InvalidOperationException("No available Open AI hosts", e);
                }

                ;
                logger.LogWarning(e, "Failed to handle request. Trying another endpoint");
            }
        } while (toTry.Count > 0);

        throw new InvalidOperationException("Failed to satisfy request");
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "Random Router",
            Endpoints = _openAiServers.Select(x => WriteDebug())
        };
    }
    
    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

}