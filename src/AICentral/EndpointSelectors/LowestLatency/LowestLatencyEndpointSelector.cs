﻿using System.Collections.Concurrent;
using AICentral.Core;
using AICentral.Endpoints;
using Microsoft.Extensions.Primitives;

namespace AICentral.EndpointSelectors.LowestLatency;

public class LowestLatencyEndpointSelector : IEndpointSelector
{
    private readonly System.Random _rnd = new(Environment.TickCount);
    private readonly IEndpointDispatcher[] _openAiServers;
    private readonly bool _logMissingModelMappingsAsInformation;

    private readonly ConcurrentDictionary<IEndpointDispatcher, ConcurrentQueue<double>> _recentLatencies =
        new();

    private const int RequiredCount = 10;

    public LowestLatencyEndpointSelector(IEndpointDispatcher[] openAiServers, bool logMissingModelMappingsAsInformation)
    {
        _openAiServers = openAiServers;
        _logMissingModelMappingsAsInformation = logMissingModelMappingsAsInformation;
    }

    public async Task<AICentralResponse> Handle(
        IRequestContext context,
        IncomingCallDetails aiCallInformation,
        bool isLastChance,
        IResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        var logger = context.GetLogger<LowestLatencyEndpointSelector>();
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
                    responseGenerator,
                    cancellationToken); //awaiting to unwrap any Aggregate Exceptions

                UpdateLatencies(logger, chosen, response.DownstreamUsageInformation);

                return response;
            }
            catch (HttpRequestException e)
            {
                if (!toTry.Any())
                {
                    logger.LogError(e, "Failed to handle request. Exhausted endpoints");
                    throw;
                }

                if (e is DownstreamRequestException { Result: MisingModelMapping mmm })
                {
                    if (mmm.LogAsWarning)
                    {
                        logger.LogWarning("Unable to dispatch to endpoint - trying another endpoint. Missing model mapping on endpoint {Endpoint} - {Model}", mmm.HostName, mmm.MissingModel);
                    }
                    else
                    {
                        logger.LogInformation("Unable to dispatch to endpoint - trying another endpoint. Missing model mapping on endpoint {Endpoint} - {Model}", mmm.HostName, mmm.MissingModel);
                    } 
                }
                else
                {
                    logger.LogWarning(e, "Failed to handle request. Trying another endpoint");
                }
            }
            finally
            {
                tried++;
            }
        }

        throw new InvalidOperationException("Failed to satisfy request");
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

    private void UpdateLatencies(ILogger<LowestLatencyEndpointSelector> logger, IEndpointDispatcher endpoint,
        DownstreamUsageInformation requestInformation)
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

    private double GetRecentAverageLatencyFor(IEndpointDispatcher endpoint)
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