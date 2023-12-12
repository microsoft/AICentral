using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using AICentral.Core;

namespace AICentral.RateLimiting;

public class TokenBasedRateLimitingProvider : RateLimitingProvider, IAICentralGenericStepFactory 
{
    private readonly TokenBasedRateLimiterOptions _rateLimiterOptions;

    public TokenBasedRateLimitingProvider(TokenBasedRateLimiterOptions rateLimiterOptions): base(rateLimiterOptions.LimitType!.Value)
    {
        _rateLimiterOptions = rateLimiterOptions;
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

    protected override PartitionedRateLimiter<HttpContext> BuildRateLimiter()
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            new RateLimitPartition<string>(GetPartitionId(ctx),
                _ => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions()
                {
                    Window = TimeSpan.FromSeconds(_rateLimiterOptions.Window!.Value),
                    PermitLimit = _rateLimiterOptions.PermitLimit!.Value,
                    AutoReplenishment = false
                })));
    }

    protected override int? UsedTokens(AICentralUsageInformation aiCentralUsageInformation)
    {
        return aiCentralUsageInformation.TotalTokens;
    }

    public override Task AdjustResponseHeaders(HttpContext context, HttpResponseHeaders responseHeaders)
    {
        return Task.CompletedTask;
    }

    public override object WriteDebug()
    {
        return new
        {
            Type = "TokenBasedRateLimiterOptions",
            Properties = _rateLimiterOptions,
        };
    }
}