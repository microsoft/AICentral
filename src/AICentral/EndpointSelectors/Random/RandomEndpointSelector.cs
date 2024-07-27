using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.EndpointSelectors.Random;

public class RandomEndpointSelector : IEndpointSelector
{
    private readonly System.Random _rnd = new(Environment.TickCount);
    private readonly IEndpointDispatcher[] _openAiServers;

    public RandomEndpointSelector(IEndpointDispatcher[] openAiServers)
    {
        _openAiServers = openAiServers;
    }

    public async Task<AICentralResponse> Handle(
        IRequestContext context,
        IncomingCallDetails aiCallInformation,
        bool isLastChance,
        IResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        var logger = context.GetLogger<RandomEndpointSelector>();
        logger.LogDebug("Random Endpoint selector is handling request");
        var count = 0;
        foreach (var chosen in NextEndpointEnumerator(context))
        {
            count++;
            var isLast = count == _openAiServers.Length;
            try
            {
                return await chosen.Handle(
                    context,
                    aiCallInformation,
                    isLastChance && isLast,
                    responseGenerator,
                    cancellationToken); //awaiting to unwrap any Aggregate Exceptions
            }
            catch (HttpRequestException e)
            {
                if (isLast)
                {
                    logger.LogError(e, "Failed to handle request. Exhausted endpoints");
                    throw;
                }

                logger.LogWarning(e, "Failed to handle request. Trying another endpoint");
            }
        }

        throw new InvalidOperationException("Failed to satisfy request");
    }

    protected virtual IEnumerable<IEndpointDispatcher> NextEndpointEnumerator(IRequestContext context)
    {
        var toTry = _openAiServers.ToList();
        var allServersCount = toTry.Count;
        for (var counter = 0; counter < allServersCount; counter++)
        {
            var chosen = toTry.ElementAt(_rnd.Next(0, toTry.Count));
            toTry.Remove(chosen);
            yield return chosen;
        }
    }

    public IEnumerable<IEndpointDispatcher> ContainedEndpoints()
    {
        return _openAiServers;
    }
    
    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders)
    {
        rawHeaders.Remove("x-ratelimit-remaining-tokens");
        rawHeaders.Remove("x-ratelimit-remaining-requests");
        return Task.CompletedTask;
    }

}