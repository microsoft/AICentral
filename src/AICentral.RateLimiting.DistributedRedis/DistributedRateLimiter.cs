using AICentral.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using StackExchange.Redis;

namespace AICentral.RateLimiting.DistributedRedis;

public class DistributedRateLimiter : IPipelineStep
{
    private readonly IDatabase _redisAsync;
    private readonly string _stepName;
    private readonly TimeSpan _window;
    private readonly int _limitPerInterval;
    private readonly LimitType _limitType;
    private readonly MetricType _metricType;
    private static readonly DateTime BaseTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DistributedRateLimiter(
        string stepName,
        IRedisAsync redisAsync,
        TimeSpan window,
        int limitPerInterval,
        LimitType limitType,
        MetricType metricType)
    {
        _redisAsync = redisAsync.Multiplexer.GetDatabase();
        _stepName = stepName;
        _window = window;
        _limitPerInterval = limitPerInterval;
        _limitType = limitType;
        _metricType = metricType;
    }

    public async Task<AICentralResponse> Handle(IRequestContext context, IncomingCallDetails aiCallInformation,
        NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        if (context.ResponseSupportsTrailers())
        {
            context.ResponseDeclareTrailer(
                _metricType == MetricType.Tokens
                    ? "x-aicentral-remaining-tokens"
                    : "x-aicentral-remaining-requests");
        }

        //which interval should we be in? And when should it end?
        var elapsedMinutesSinceMin = DateTime.UtcNow - BaseTime;
        var intervalNumber = Math.Floor(elapsedMinutesSinceMin.TotalSeconds / _window.TotalSeconds);
        var intervalEnd = BaseTime.Add(TimeSpan.FromSeconds((1 + intervalNumber) * _window.TotalSeconds));

        var keyPartFromLimitType =
            _limitType == LimitType.PerConsumer
                ? context.UserName ?? "all"
                : string.Empty;

        var keyName =
            $"{aiCallInformation.PipelineName}-{_stepName}-{_metricType}-{_limitType}-{_window.TotalSeconds}-{intervalNumber}-{keyPartFromLimitType}";
        var key = new RedisKey(keyName);

        var thisNodeLimitConsumed = (int?)_redisAsync.HashGet(key, Environment.MachineName);
        var keyValue = await _redisAsync.HashValuesAsync(key);
        var limitConsumed = keyValue.Sum(x => (int)x);

        if (limitConsumed > _limitPerInterval)
        {
            var resultHandler = Results.StatusCode(429);
            context.ResponseSetHeader(HeaderNames.RetryAfter, intervalEnd.ToString("R"));

            return new AICentralResponse(
                DownstreamUsageInformation.Empty(
                    context,
                    aiCallInformation,
                    string.Empty,
                    null
                ), resultHandler);
        }

        var response = await next(context, aiCallInformation, cancellationToken);

        if (response.DownstreamUsageInformation.Success.GetValueOrDefault())
        {
            //update the cache with the token count
            if (_metricType == MetricType.Requests || response.DownstreamUsageInformation.TotalTokens.HasValue)
            {
                var consumed = _metricType == MetricType.Requests
                    ? thisNodeLimitConsumed.GetValueOrDefault() + 1
                    : thisNodeLimitConsumed.GetValueOrDefault() +
                      response.DownstreamUsageInformation.TotalTokens!.Value;

                var success = await _redisAsync.HashSetAsync(
                    key,
                    new RedisValue(Environment.MachineName),
                    consumed
                );

                //Can't set the expiry directly on the cache, but let's update it to when it should expire. 
                //Hopefully this doesn't have too big an impact on performance...
                _redisAsync.KeyExpire(key, intervalEnd);

                if (context.ResponseSupportsTrailers())
                {
                    context.ResponseAppendTrailer(
                        _metricType == MetricType.Tokens
                            ? "x-aicentral-remaining-tokens"
                            : "x-aicentral-remaining-requests",
                        new StringValues(consumed.ToString())!);
                }
            }
        }

        return response;
    }

    /// <summary>
    /// We can't return remaining token headers yet as streamed responses haven't been calculated (at this point).
    /// Instead we will return a trailing header directly in the response (see above method).
    /// </summary>
    /// <param name="context"></param>
    /// <param name="rawResponse"></param>
    /// <param name="rawHeaders"></param>
    /// <returns></returns>
    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}