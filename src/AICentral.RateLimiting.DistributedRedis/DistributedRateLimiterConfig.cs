namespace AICentral.RateLimiting.DistributedRedis;

public class DistributedRateLimiterConfig
{
    public string? RedisConfiguration { get; set; }
    public int? PermitLimit { get; set; }
    public TimeSpan? Window { get; set; }
    public MetricType? MetricType { get; set; }
    public LimitType? LimitType { get; set; }
}