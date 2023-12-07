using System.Threading.RateLimiting;

namespace AICentral.Steps.RateLimiting;

public class AICentralFixedWindowRateLimiterOptions
{
    public FixedWindowRateLimitingLimitType? LimitType { get; set; }
    public FixedWindowRateLimiterOptions? Options { get; set; }
}