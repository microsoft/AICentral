using System.Threading.RateLimiting;

namespace AICentral.RateLimiting;

public class AICentralFixedWindowRateLimiterOptions
{
    public FixedWindowRateLimitingLimitType? LimitType { get; set; }
    public FixedWindowRateLimiterOptions? Options { get; set; }
}