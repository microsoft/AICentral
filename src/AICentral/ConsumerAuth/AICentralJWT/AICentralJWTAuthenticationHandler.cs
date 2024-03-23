using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AICentral.ConsumerAuth.AICentralJWT;

internal class AICentralJwtAuthenticationHandler(
    IOptionsMonitor<AICentralJwtAuthProviderConfig> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AICentralJwtAuthProviderConfig>(options, logger, encoder)
{
    private RsaSecurityKey? _key;

    protected override Task InitializeHandlerAsync()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(Options.PublicKeyPem);
        _key = new RsaSecurityKey(rsa)
        {
            KeyId = "0"
        };
        return Task.CompletedTask;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue("api-key", out var key))
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var result = await tokenHandler.ValidateTokenAsync(
                key.ToString(),
                new TokenValidationParameters()
                {
                    IssuerSigningKey = _key!,
                    ValidateAudience = true,
                    ValidAudience = Options.TokenIssuer,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ValidIssuer = Options.TokenIssuer
                });

            if (result.IsValid)
            {
                var identity = result.ClaimsIdentity;
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
        }

        return AuthenticateResult.Fail("Invalid or missing AICentralJwt api-key");
    }
}