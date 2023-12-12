namespace AICentral.RateLimiting;

public class TokenBasedRateLimiterOptions
{
    public RateLimitingLimitType? LimitType { get; set; }
    public int? Window { get; set; }
    public int? PermitLimit { get; set; }
}