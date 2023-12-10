using System.Collections.Concurrent;
using System.Net.Http.Headers;
using AICentral.Core;
using AICentral.Steps.TokenBasedRateLimiting;

namespace AICentral.Steps.Endpoints;

public class InMemoryRateLimitingTracker
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _rateLimiters = new();

    public InMemoryRateLimitingTracker(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public void RateLimiting(string serverName, RetryConditionHeaderValue? until)
    {
        serverName = serverName.ToLowerInvariant();
        DateTimeOffset retryAt = _dateTimeProvider.Now.AddSeconds(15);
        if (until != null && (until.Date != null || until.Delta != null))
        {
            retryAt = until.Date ?? _dateTimeProvider.Now.Add(until.Delta!.Value);
        }

        _rateLimiters.AddOrUpdate(serverName, retryAt, (_, lastUpdate) => lastUpdate > retryAt ? lastUpdate : retryAt);
    }

    public bool IsRateLimiting(string serverName, out DateTimeOffset? until)
    {
        serverName = serverName.ToLowerInvariant();
        if (_rateLimiters.TryGetValue(serverName, out var limitUntil))
        {
            if (limitUntil > _dateTimeProvider.Now)
            {
                until = limitUntil;
                return true;
            }

            _rateLimiters.TryRemove(serverName, out _);
        }

        until = null;
        return false;
    }
}