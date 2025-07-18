using System.Net.Http.Headers;
using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI.Authorisers;

public class BearerTokenPassThroughAuth : IEndpointAuthorisationHandler
{
    public virtual Task ApplyAuthorisationToRequest(IRequestContext incomingRequest,
        HttpRequestMessage outgoingRequest)
    {
        var authHeader = incomingRequest.RequestHeaders.Authorization.FirstOrDefault() ?? string.Empty;
        
        //TODO - add a config section to opt-in to this. Don't want to change existing behaviour.
        var apiKeyHeader = incomingRequest.RequestHeaders["api-key"].FirstOrDefault();

        authHeader = string.IsNullOrWhiteSpace(authHeader) && string.IsNullOrWhiteSpace(apiKeyHeader)
            ? throw new ArgumentException("Bearer Token Pass Through. Could not find auth header on incoming request") : authHeader;

        if (authHeader.StartsWith("bearer", StringComparison.InvariantCultureIgnoreCase))
        {
            var parts = authHeader.Split(" ");
            if (parts.Length != 2)
            {
                //fallback?
                if (!string.IsNullOrWhiteSpace(apiKeyHeader))
                {
                    outgoingRequest.Headers.TryAddWithoutValidation("api-key", apiKeyHeader);
                }
                else
                {
                    throw new ArgumentException(
                        "Bearer Token Pass Through. Unexpected Authorisation scheme on incoming request");
                }
            }
            else
            {
                outgoingRequest.Headers.Authorization = new AuthenticationHeaderValue(parts[0], parts[1]);
            }
        }

        if (!string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            outgoingRequest.Headers.Add("api-key", apiKeyHeader);
        }

        return Task.CompletedTask;
    }

    public object WriteDebug()
    {
        return new { Type = "Bearer Token Pass-Through" };
    }
}
