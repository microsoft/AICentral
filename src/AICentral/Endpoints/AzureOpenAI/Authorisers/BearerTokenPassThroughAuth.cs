using System.Net.Http.Headers;
using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI.Authorisers;

public class BearerTokenPassThroughAuth : IEndpointAuthorisationHandler
{
    public virtual Task ApplyAuthorisationToRequest(IRequestContext incomingRequest,
        HttpRequestMessage outgoingRequest)
    {
        var authHeader = incomingRequest.RequestHeaders.Authorization.FirstOrDefault() ?? string.Empty;

        if (authHeader.StartsWith("bearer", StringComparison.InvariantCultureIgnoreCase))
        {
            var parts = authHeader.Split(" ");
            if (parts.Length != 2)
            {
                throw new ArgumentException(
                    "Bearer Token Pass Through. Unexpected Authorisation scheme on incoming request");
            }

            outgoingRequest.Headers.Authorization = new AuthenticationHeaderValue(parts[0], parts[1]);
        }

        return Task.CompletedTask;
    }

    public object WriteDebug()
    {
        return new { Type = "Bearer Token Pass-Through" };
    }
}