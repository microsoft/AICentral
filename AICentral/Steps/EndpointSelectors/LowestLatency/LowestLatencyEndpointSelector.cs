using System.Collections.Concurrent;
using AICentral.Core;
using AICentral.Steps.Endpoints;

namespace AICentral.Steps.EndpointSelectors.LowestLatency;

public class LowestLatencyEndpointSelector : IEndpointSelector
{
    private readonly System.Random _rnd = new(Environment.TickCount);
    private readonly IAICentralEndpointDispatcher[] _openAiServers;

    private readonly ConcurrentDictionary<IAICentralEndpointDispatcher, ConcurrentQueue<double>> _recentLatencies =
        new();

    private const int RequiredCount = 10;

    public LowestLatencyEndpointSelector(IAICentralEndpointDispatcher[] openAiServers)
    {
        _openAiServers = openAiServers;
    }

    public async Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation aiCallInformation,
        bool isLastChance,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<LowestLatencyEndpointSelector>>();
        var toTry = _openAiServers.OrderBy(GetRecentAverageLatencyFor).ToArray();
        logger.LogDebug("Lowest Latency selector is handling request");
        var tried = 0;
        foreach (var chosen in toTry)
        {
            try
            {
                var response = await chosen.Handle(
                    context,
                    aiCallInformation,
                    isLastChance && (tried == toTry.Length - 1),
                    cancellationToken); //awaiting to unwrap any Aggregate Exceptions

                UpdateLatencies(logger, chosen, response.AICentralUsageInformation);

                return response;
            }
            catch (HttpRequestException e)
            {
                if (!toTry.Any())
                {
                    logger.LogError(e, "Failed to handle request. Exhausted endpoints");
                    throw new InvalidOperationException("No available Open AI hosts", e);
                }

                logger.LogWarning(e, "Failed to handle request. Trying another endpoint");
            }
            finally
            {
                tried++;
            }
        }

        throw new InvalidOperationException("Failed to satisfy request");
    }

    private void UpdateLatencies(ILogger<LowestLatencyEndpointSelector> logger, IAICentralEndpointDispatcher endpoint,
        AICentralUsageInformation requestInformation)
    {
        if (!_recentLatencies.ContainsKey(endpoint))
        {
            _recentLatencies[endpoint] = new ConcurrentQueue<double>();
        }

        _recentLatencies[endpoint].Enqueue(requestInformation.Duration.TotalMilliseconds);
        logger.LogDebug("Endpoint {Endpoint} has a latency of {Latency}ms", requestInformation.OpenAIHost, requestInformation.Duration.TotalMilliseconds);

        //only hold onto a specified number of items
        var currentCount = _recentLatencies[endpoint].Count;
        if (currentCount > RequiredCount)
        {
            var toRemove = currentCount - RequiredCount;
            for (var count = 0; count < toRemove; count++)
            {
                _recentLatencies[endpoint].TryDequeue(out _);
            }
        }
    }

    private double GetRecentAverageLatencyFor(IAICentralEndpointDispatcher endpoint)
    {
        var hasLatencyData = _recentLatencies.TryGetValue(endpoint, out var queue);
        if (!hasLatencyData)
        {
            //try and get some data. Might need to check failure count here as-well, although the circuit breaker should ensure often failing endpoint gives up quickly.
            return _rnd.Next(0, 5);
        }

        if (queue!.Count < RequiredCount)
        {
            //not enough data so keep it pretty high so we can fill in some numbers.
            return _rnd.Next(0, 5);
        }

        return queue.Sum() / queue.Count;
    }
}