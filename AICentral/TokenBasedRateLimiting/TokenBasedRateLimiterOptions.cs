namespace AICentral.TokenBasedRateLimiting;

public class TokenBasedRateLimiterOptions
{
    public TokenBasedRateLimitingLimitType? LimitType { get; set; }
    public int? Window { get; set; }
    public int? PermitLimit { get; set; }
}