using System.Collections.Concurrent;
using System.Net.Http.Headers;
using AICentral.Core;

namespace AICentral.Endpoints;

internal class DownstreamEndpointResponseDataTracker
{
    private class LastSeenValues
    {
        public required DateTimeOffset LastSeenAt { get; init; }
        public required long Amount { get; set; }
    }

    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _rateLimiters = new();
    private readonly ConcurrentDictionary<string, LastSeenValues> _lastSeenRemainingRequests = new();
    private readonly ConcurrentDictionary<string, LastSeenValues> _lastSeenRemainingTokens = new();

    public DownstreamEndpointResponseDataTracker(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public void RateLimiting(string serverName, RetryConditionHeaderValue? until)
    {
        serverName = serverName.ToLowerInvariant();
        DateTimeOffset retryAt = _dateTimeProvider.Now.AddSeconds(15);
        if (until != null && (until.Date != null || until.Delta != null))
        {
            var timeBeforeRetry = until.Date == null ? until.Delta!.Value : until.Date!.Value - _dateTimeProvider.Now;
            
            if (timeBeforeRetry > TimeSpan.FromHours(1))
            {
                //sometimes we get wobbly responses back with extremely large retry times. Ignore this and wait for something more sensible!
                return;
            }

            retryAt = until.Date ?? _dateTimeProvider.Now.Add(until.Delta!.Value);
        }

        _rateLimiters.AddOrUpdate(serverName, retryAt, (_, lastUpdate) => lastUpdate > retryAt ? lastUpdate : retryAt);
    }

    public void RecordMetrics(string serverName, long? remainingTokens, long? remainingRequests)
    {
        var now = _dateTimeProvider.Now;
        
        if (remainingTokens != null)
        {
            UpdateValue(_lastSeenRemainingTokens, serverName, remainingTokens.Value, now);
        }
        if (remainingRequests != null)
        {
            UpdateValue(_lastSeenRemainingRequests, serverName, remainingRequests.Value, now);
        }
    }

    public IEnumerable<string> PrioritiseBasedOnMetrics(DateTimeOffset now, string[] endpoints)
    {
        return endpoints.OrderByDescending(x =>
            {
                if (!_lastSeenRemainingTokens.TryGetValue(x, out var tokenMetrics))
                {
                    return long.MaxValue;
                }

                //These numbers should eventually fade... Give a minute and then consider the numbers outdated 
                if (tokenMetrics.LastSeenAt.AddMinutes(1) < now)
                {
                    return long.MaxValue;
                }
                
                return tokenMetrics.Amount;
            })
            .ThenByDescending(x =>
            {
                if (!_lastSeenRemainingRequests.TryGetValue(x, out var requestMetrics))
                {
                    return long.MaxValue;
                }

                //These numbers should eventually fade... Give a minute and then consider the numbers outdated 
                if (requestMetrics.LastSeenAt.AddMinutes(1) < now)
                {
                    return long.MaxValue;
                }
                
                return requestMetrics.Amount;
            });
    }

    private void UpdateValue(ConcurrentDictionary<string, LastSeenValues> dictionary, string serverName, long remainingTokens, DateTimeOffset now)
    {
        dictionary.AddOrUpdate(
            serverName,
            s => new LastSeenValues() { Amount = remainingTokens, LastSeenAt = now },
            (s, x) =>
            {
                if (x.LastSeenAt < now)
                {
                    return new LastSeenValues() { Amount = remainingTokens, LastSeenAt = now };
                }

                return x;
            });
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