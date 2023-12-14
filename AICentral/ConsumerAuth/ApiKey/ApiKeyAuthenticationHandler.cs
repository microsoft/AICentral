using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AICentral.ConsumerAuth.ApiKey;

internal class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyOptions>
{
    private readonly IOptionsMonitor<ApiKeyOptions> _options;

    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
        _options = options;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue(Options.HeaderName, out var key))
        {
            var client = Options.Clients.SingleOrDefault(x => x.Key1 == key || x.Key2 == key);
            if (client != null)
            {
                var claims = new[] { new Claim(ClaimTypes.Name, client.ClientName!) };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }

        return Task.FromResult(AuthenticateResult.Fail($"Invalid or missing {Options.HeaderName} key"));
    }
}