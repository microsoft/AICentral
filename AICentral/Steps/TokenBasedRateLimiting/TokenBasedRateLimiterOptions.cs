namespace AICentral.Steps.TokenBasedRateLimiting;

public class TokenBasedRateLimiterOptions
{
    public TokenBasedRateLimitingLimitType? LimitType { get; set; }
    public int? TokenWindowInSeconds { get; set; }
    public int? TokenLimit { get; set; }
}