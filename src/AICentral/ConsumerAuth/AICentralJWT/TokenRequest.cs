namespace AICentral.ConsumerAuth.AICentralJWT;

public class TokenRequest
{
    public string[] Names { get; set; } = default!;
    public string[] ValidPipelines { get; set; } = default!;
    public TimeSpan ValidFor { get; set; } = default!;
}