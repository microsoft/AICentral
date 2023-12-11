using Microsoft.AspNetCore.Authentication;

namespace AICentral.Steps.Auth.ApiKey;

internal class ApiKeyOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = default!;
    public ApiKeyClientAuthClientConfig[] Clients { get; set; } = default!;
}