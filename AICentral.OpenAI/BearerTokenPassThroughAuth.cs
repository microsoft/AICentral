using System.Net.Http.Headers;
using AICentral.Core;
using Microsoft.AspNetCore.Http;

namespace AICentral.OpenAI;

public class BearerTokenPassThroughAuth : IEndpointAuthorisationHandler
{
    public Task ApplyAuthorisationToRequest(HttpRequest incomingRequest,
        HttpRequestMessage outgoingRequest)
    {
        var authHeader = incomingRequest.Headers.Authorization.FirstOrDefault();

        authHeader = string.IsNullOrWhiteSpace(authHeader)
            ? throw new ArgumentException("Bearer Token Pass Through. Could not find auth header on incoming request") : authHeader;

        var parts = authHeader.Split("Bearer ");
        if (parts.Length != 2)
        {
            throw new ArgumentException("Bearer Token Pass Through. Unexpected Authorisation scheme on incoming request");
        }

        outgoingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", parts[1]);

        return Task.CompletedTask;
    }

    public object WriteDebug()
    {
        return new { Type = "Bearer Token Pass-Through" };
    }
}