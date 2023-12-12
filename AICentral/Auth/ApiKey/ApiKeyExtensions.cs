using Microsoft.AspNetCore.Authentication;

namespace AICentral.Auth.ApiKey;

internal static class ApiKeyExtensions
{
    public static void AddApiKeyAuth(this AuthenticationBuilder builder, string id, string apiKey1, string apiKey2)
    {
    }
}