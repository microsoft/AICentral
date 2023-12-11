using System.Globalization;
using System.Threading.RateLimiting;
using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.TokenBasedRateLimiting;

public class TokenBasedRateLimitingProvider : IAICentralGenericStepFactory, IAICentralPipelineStep
{
    private readonly TokenBasedRateLimiterOptions _rateLimiterOptions;
    private readonly PartitionedRateLimiter<HttpContext> _rateLimiter;

    public TokenBasedRateLimitingProvider(TokenBasedRateLimiterOptions fixedWindowRateLimiterOptions)
    {
        _rateLimiterOptions = fixedWindowRateLimiterOptions;
        _rateLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            new RateLimitPartition<string>(GetPartitionId(ctx),
                _ => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions()
                {
                    Window = TimeSpan.FromSeconds(_rateLimiterOptions.Window!.Value),
                    PermitLimit = _rateLimiterOptions.PermitLimit!.Value,
                    AutoReplenishment = false
                })));
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

    public static string ConfigName => "TokenBasedRateLimiting";

    public static IAICentralGenericStepFactory BuildFromConfig(
        ILogger logger,
        AICentralTypeAndNameConfig config)
    {
        var properties = config.TypedProperties<TokenBasedRateLimiterOptions>()!;
        Guard.NotNull(properties, "Properties");

        return new TokenBasedRateLimitingProvider(properties);
    }

    public IAICentralPipelineStep Build()
    {
        return this;
    }

    public async Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<TokenBasedRateLimitingProvider>>();
        if (HasExceededTokenLimit(context, out var retryAt))
        {
            logger.LogDebug("Detected token limit breach for {User}. Retry available in {Retry}",
                context.User.Identity?.Name ?? "unknown", retryAt ?? TimeSpan.Zero);
            var resultHandler = Results.StatusCode(429);
            if (retryAt != null)
            {
                context.Response.Headers.RetryAfter = new StringValues(retryAt.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
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

        var result = await pipeline.Next(context, aiCallInformation, cancellationToken);
        if (!result.AICentralUsageInformation.TotalTokens.HasValue)
        {
            return result;
        }

        var rateLimiterStatistics = _rateLimiter.GetStatistics(context);
        using var _ = _rateLimiter.AttemptAcquire(
            context,
            Math.Min(Convert.ToInt32(rateLimiterStatistics?.CurrentAvailablePermits ?? 0),
                result.AICentralUsageInformation.TotalTokens.Value));

        logger.LogDebug("New tokens consumed by {User}. New Count {Count}",
            context.User.Identity?.Name ?? "unknown", rateLimiterStatistics?.CurrentAvailablePermits + result.AICentralUsageInformation.TotalTokens.Value);

        return result;
    }

    private bool HasExceededTokenLimit(HttpContext context, out TimeSpan? retryAfter)
    {
        using var lease = _rateLimiter.AttemptAcquire(context, 0);
        lease.TryGetMetadata(MetadataName.RetryAfter.Name, out object? retry);
        retryAfter = retry as TimeSpan?;
        return !lease.IsAcquired;
    }

    private string GetPartitionId(HttpContext context)
    {
        var id = _rateLimiterOptions.LimitType == TokenBasedRateLimitingLimitType.PerAICentralEndpoint
            ? "__endpoint"
            : context.User.Identity?.Name ?? "unknown";
        return id;
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "TokenBasedRateLimiterOptions",
            Properties = _rateLimiterOptions,
        };
    }
}