using Microsoft.AspNetCore.Authentication;

namespace AICentral.ConsumerAuth.AICentralJWT;

public class AICentralJwtAuthProviderConfig: AuthenticationSchemeOptions
{
    public string? AdminKey { get; set; }
    public string? TokenIssuer { get; set; }
    public string? PrivateKeyPem { get; set; }
    public string? PublicKeyPem { get; set; }
    public string[] ValidPipelines { get; set; }
}