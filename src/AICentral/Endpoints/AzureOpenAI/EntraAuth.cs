using System.Net.Http.Headers;
using AICentral.Core;
using Azure.Core;
using Azure.Identity;

namespace AICentral.Endpoints.AzureOpenAI;

public class EntraAuth : IEndpointAuthorisationHandler
{
    public async Task ApplyAuthorisationToRequest(HttpRequest incomingRequest,
        HttpRequestMessage outgoingRequest)
    {
        var token = (await new DefaultAzureCredential().GetTokenAsync(
            new TokenRequestContext(new[] { "https://cognitiveservices.azure.com" }))).Token;
        outgoingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public object WriteDebug()
    {
        return new { Type = "Entra (Default Azure Credential)" };
    }
}
