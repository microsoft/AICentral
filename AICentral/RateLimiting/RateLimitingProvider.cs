using System.Globalization;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.RateLimiting;

public abstract class RateLimitingProvider : IAICentralPipelineStep
{
    private readonly RateLimitingLimitType _rateLimitingLimitType;
    private readonly PartitionedRateLimiter<HttpContext> _rateLimiter;

    protected RateLimitingProvider(RateLimitingLimitType rateLimitingLimitType)
    {
        _rateLimitingLimitType = rateLimitingLimitType;
        // ReSharper disable once VirtualMemberCallInConstructor
        _rateLimiter = BuildRateLimiter();
    }

    protected abstract PartitionedRateLimiter<HttpContext> BuildRateLimiter();

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

    public IAICentralPipelineStep Build()
    {
        return this;
    }

    public async Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<RateLimitingProvider>>();
        if (HasExceededTokenLimit(context, out var retryAt))
        {
            return ExceededRateLimitResponse(context, aiCallInformation, logger, retryAt);
        }

        var result = await pipeline.Next(context, aiCallInformation, cancellationToken);

        var tokensConsumed = UsedTokens(result.AICentralUsageInformation);

        var rateLimiterStatistics = _rateLimiter.GetStatistics(context);
        using var _ = _rateLimiter.AttemptAcquire(
            context,
            Math.Min(
                Convert.ToInt32(rateLimiterStatistics?.CurrentAvailablePermits ?? 0),
                tokensConsumed!.Value));

        logger.LogDebug("New tokens consumed by {User}. New Count {Count}",
            context.User.Identity?.Name ?? "unknown",
            rateLimiterStatistics?.CurrentAvailablePermits - tokensConsumed!.Value);

        return result;
    }

    public Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return CustomBuildResponseHeaders(context, rawHeaders);
    }

    protected abstract Task CustomBuildResponseHeaders(HttpContext context, Dictionary<string, StringValues> rawHeaders);

    protected abstract int? UsedTokens(AICentralUsageInformation aiCentralUsageInformation);

    private static AICentralResponse ExceededRateLimitResponse(HttpContext context, AICallInformation aiCallInformation,
        ILogger<RateLimitingProvider> logger, TimeSpan? retryAt)
    {
        logger.LogDebug("Detected token limit breach for {User}. Retry available in {Retry}",
            context.User.Identity?.Name ?? "unknown", retryAt ?? TimeSpan.Zero);

        var resultHandler = Results.StatusCode(429);
        if (retryAt != null)
        {
            context.Response.Headers.RetryAfter =
                new StringValues(retryAt.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }

        var dateTimeProvider = context.RequestServices.GetRequiredService<IDateTimeProvider>();
        return new AICentralResponse(
            new AICentralUsageInformation(
                string.Empty,
                aiCallInformation.IncomingCallDetails.IncomingModelName,
                context.User.Identity?.Name ?? string.Empty,
                aiCallInformation.IncomingCallDetails.AICallType,
                null, null, null, null, null, null, null,
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty, dateTimeProvider.Now, TimeSpan.Zero),
            resultHandler);
    }

    private bool HasExceededTokenLimit(HttpContext context, out TimeSpan? retryAfter)
    {
        using var lease = _rateLimiter.AttemptAcquire(context, 0);
        lease.TryGetMetadata(MetadataName.RetryAfter.Name, out object? retry);
        retryAfter = retry as TimeSpan?;
        return !lease.IsAcquired;
    }

    protected long RemainingUnits(HttpContext context)
    {
        var lease = _rateLimiter.GetStatistics(context);
        return lease?.CurrentAvailablePermits ?? 0;
    }

    protected string GetPartitionId(HttpContext context)
    {
        var id = _rateLimitingLimitType == RateLimitingLimitType.PerAICentralEndpoint
            ? "__endpoint"
            : context.User.Identity?.Name ?? "unknown";
        return id;
    }

    public abstract object WriteDebug();
}