using AICentral.Pipelines.Endpoints;

namespace AICentral.Pipelines.EndpointSelectors.Random;

public class RandomEndpointSelector: IAICentralEndpointSelector
{
    private readonly IAICentralEndpoint[] _openAiServers;
    private readonly System.Random _rnd = new(Environment.TickCount);

    public RandomEndpointSelector(IList<IAICentralEndpoint> openAiServers)
    {
        _openAiServers = openAiServers.ToArray();
        
    }
    public async Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<RandomEndpointSelector>>();
        var toTry = _openAiServers.ToList();
        logger.LogDebug("Random Endpoint selector is handling request");
        do
        {
            var chosen = toTry.ElementAt(_rnd.Next(0, toTry.Count));
            toTry.Remove(chosen);
            try
            {
                return await chosen.Handle(context, pipeline, cancellationToken);
            }
            catch (Exception e)
            {
                if (!toTry.Any())
                {
                    logger.LogError(e, "Failed to handle request. Exhausted endpoints");
                    throw new InvalidOperationException("No available Open AI hosts", e);
                };
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

    public static string ConfigName => "RoundRobinCluster";

    public static IAICentralEndpointSelector BuildFromConfig(Dictionary<string, string> parameters, Dictionary<string, IAICentralEndpoint> endpoints)
    {
        return new RandomEndpointSelector(
            parameters["Endpoints"].Split(',').Select(x => endpoints[x])
            .ToArray());
    }
}
