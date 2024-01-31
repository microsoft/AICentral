using System.Globalization;
using System.Threading.RateLimiting;
using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.RateLimiting;

public class FixedWindowRateLimitingProvider : IAICentralPipelineStep, IAICentralGenericStepFactory
{
    private readonly FixedWindowRateLimiterOptions _rateLimiterOptions;
    private readonly PartitionedRateLimiter<HttpContext> _rateLimiter;

    public FixedWindowRateLimitingProvider(FixedWindowRateLimiterOptions fixedWindowRateLimiterOptions)
    {
        _rateLimiterOptions = fixedWindowRateLimiterOptions;
        _rateLimiter = BuildRateLimiter();
    }

    public async Task<AICentralResponse> Handle(HttpContext context, IncomingCallDetails aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<FixedWindowRateLimitingProvider>>();
        if (HasExceededTokenLimit(context, out var retryAt))
        {
            return ExceededRateLimitResponse(context, aiCallInformation, logger, retryAt);
        }

        var result = await pipeline.Next(context, aiCallInformation, cancellationToken);

        var tokensConsumed = UsedTokens(result.DownstreamUsageInformation);

        if (tokensConsumed.HasValue)
        {
            var rateLimiterStatistics = _rateLimiter.GetStatistics(context);
            using var _ = _rateLimiter.AttemptAcquire(
                context,
                Math.Min(
                    Convert.ToInt32(rateLimiterStatistics?.CurrentAvailablePermits ?? 0),
                    tokensConsumed.Value));

            logger.LogDebug("New tokens consumed by {User}. New Count {Count}",
                context.User.Identity?.Name ?? "unknown",
                rateLimiterStatistics?.CurrentAvailablePermits - tokensConsumed!.Value);
        }

        return result;
    }


    public static string ConfigName => "AspNetCoreFixedWindowRateLimiting";

    public void RegisterServices(IServiceCollection services)
    {
    }

    public IAICentralPipelineStep Build(IServiceProvider serviceProvider)
    {
        return this;
    }

    public static IAICentralGenericStepFactory BuildFromConfig(
        ILogger logger,
        AICentralTypeAndNameConfig config)
    {
        var properties = config.TypedProperties<FixedWindowRateLimiterOptions>()!;
        Guard.NotNull(properties, "Properties");
        Guard.NotNull(properties.MetricType, nameof(properties.MetricType));
        Guard.NotNull(properties.LimitType, nameof(properties.LimitType));
        Guard.NotNull(properties.Options, nameof(properties.Options));

        properties.Options!.AutoReplenishment = false;
        return new FixedWindowRateLimitingProvider(properties);
    }

    private PartitionedRateLimiter<HttpContext> BuildRateLimiter()
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            new RateLimitPartition<string>(GetPartitionId(ctx),
                _ => new FixedWindowRateLimiter(_rateLimiterOptions.Options!)));
    }

    private static AICentralResponse ExceededRateLimitResponse(
        HttpContext context,
        IncomingCallDetails aiCallInformation,
        ILogger<FixedWindowRateLimitingProvider> logger,
        TimeSpan? retryAt)
    {
        logger.LogDebug("Detected token limit breach for {User}. Retry available in {Retry}",
            context.User.Identity?.Name ?? "unknown", retryAt ?? TimeSpan.Zero);

        var resultHandler = Results.StatusCode(429);
        if (retryAt != null)
        {
            context.Response.Headers.RetryAfter =
                new StringValues(retryAt.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }

        return new AICentralResponse(
            DownstreamUsageInformation.Empty(
                context, 
                aiCallInformation, 
                null, 
                string.Empty),
            resultHandler);
    }

    private bool HasExceededTokenLimit(HttpContext context, out TimeSpan? retryAfter)
    {
        using var lease = _rateLimiter.AttemptAcquire(context, 0);
        lease.TryGetMetadata(MetadataName.RetryAfter.Name, out object? retry);
        retryAfter = retry as TimeSpan?;
        return !lease.IsAcquired;
    }

    private long RemainingUnits(HttpContext context)
    {
        var lease = _rateLimiter.GetStatistics(context);
        return lease?.CurrentAvailablePermits ?? 0;
    }

    private string GetPartitionId(HttpContext context)
    {
        var id = _rateLimiterOptions.LimitType == RateLimitingLimitType.PerAICentralEndpoint
            ? "__endpoint"
            : context.User.Identity?.Name ?? "unknown";
        return id;
    }

    public Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        var key =
            $"x-ratelimit-remaining-{(_rateLimiterOptions.MetricType! == RateLimitingMetricType.Requests ? "requests" : "tokens")}";
        rawHeaders.Remove(key);
        rawHeaders.Add(key, RemainingUnits(context).ToString());
        return Task.CompletedTask;
    }

    private int? UsedTokens(DownstreamUsageInformation downstreamUsageInformation)
    {
        return _rateLimiterOptions.MetricType == RateLimitingMetricType.Requests
            ? 1
            : downstreamUsageInformation.TotalTokens;
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "FixedWindowRateLimiter",
            Properties = _rateLimiterOptions
        };
    }

    public void ConfigureRoute(WebApplication webApplication, IEndpointConventionBuilder route)
    {
    }
}