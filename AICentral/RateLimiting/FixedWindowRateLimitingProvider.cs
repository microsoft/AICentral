using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using AICentral.Core;

namespace AICentral.RateLimiting;

public class FixedWindowRateLimitingProvider : RateLimitingProvider, IAICentralGenericStepFactory
{
    private readonly AICentralFixedWindowRateLimiterOptions _rateLimiterOptions;

    public FixedWindowRateLimitingProvider(AICentralFixedWindowRateLimiterOptions aiCentralFixedWindowRateLimiterOptions): base(aiCentralFixedWindowRateLimiterOptions.LimitType!.Value)
    {
        _rateLimiterOptions = aiCentralFixedWindowRateLimiterOptions;
    }

    public static string ConfigName => "AspNetCoreFixedWindowRateLimiting";

    public static IAICentralGenericStepFactory BuildFromConfig(
        ILogger logger,
        AICentralTypeAndNameConfig config)
    {
        var properties = config.TypedProperties<AICentralFixedWindowRateLimiterOptions>()!;
        Guard.NotNull(properties, "Properties");
        Guard.NotNull(properties.LimitType, nameof(properties.LimitType));
        Guard.NotNull(properties.Options, nameof(properties.Options));

        return new FixedWindowRateLimitingProvider(properties);
    }

    protected override PartitionedRateLimiter<HttpContext> BuildRateLimiter()
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            new RateLimitPartition<string>(GetPartitionId(ctx),
                _ => new FixedWindowRateLimiter(_rateLimiterOptions.Options!)));
    }

    protected override int? UsedTokens(AICentralUsageInformation aiCentralUsageInformation)
    {
        return 1;
    }
    
    public override Task AdjustResponseHeaders(HttpContext context, HttpResponseHeaders responseHeaders)
    {
        responseHeaders.Remove("x-ratelimit-remaining-requests");
        responseHeaders.TryAddWithoutValidation("x-ratelimit-remaining-requests", RemainingUnits(context).ToString());
        return Task.CompletedTask;
    }

    public override object WriteDebug()
    {
        return new
        {
            Type = "FixedWindowRateLimiter",
            Properties = _rateLimiterOptions
        };
    }
}