namespace AICentral.RateLimiting;

public class FixedWindowRateLimiterOptions
{
    public RateLimitingMetricType? MetricType { get; set; }
    public RateLimitingLimitType? LimitType { get; set; }
    public System.Threading.RateLimiting.FixedWindowRateLimiterOptions? Options { get; set; }
}