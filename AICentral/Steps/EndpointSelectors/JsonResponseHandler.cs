using System.Net;
using AICentral.Steps.Endpoints.ResultHandlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral.Steps.EndpointSelectors;

public static class JsonResponseHandler
{
    public static async Task<AICentralResponse> Handle(HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        AICentralRequestInformation requestInformation)
    {
        var rawResponse = await openAiResponse.Content.ReadAsStringAsync(cancellationToken);
        var response = (JObject)JsonConvert.DeserializeObject(rawResponse)!;


        if (openAiResponse.StatusCode == HttpStatusCode.OK)
        {
            var model = response.Value<string>("model") ?? string.Empty;
            var usage = response["usage"];
            var promptTokens = usage?.Value<int>("prompt_tokens") ?? 0;
            var totalTokens = usage?.Value<int>("total_tokens") ?? 0;
            var completionTokens = usage?.Value<int>("completion_tokens") ?? 0;
            var responseContent = response?["choices"]?.FirstOrDefault()?["message"]?.Value<string>("content") ?? string.Empty;

            var chatRequestInformation = new AICentralUsageInformation(
                requestInformation.LanguageUrl,
                model,
                context.User.Identity?.Name ?? "unknown",
                requestInformation.CallType,
                requestInformation.Prompt,
                responseContent,
                0,
                0,
                promptTokens,
                completionTokens,
                totalTokens,
                context.Connection.RemoteIpAddress?.ToString() ?? "",
                requestInformation.StartDate,
                requestInformation.Duration);

            return new AICentralResponse(
                chatRequestInformation,
                new JsonResultHandler(openAiResponse, chatRequestInformation));
        }
        else
        {
            var chatRequestInformation = new AICentralUsageInformation(
                requestInformation.LanguageUrl,
                string.Empty,
                context.User.Identity?.Name ?? "unknown",
                requestInformation.CallType,
                requestInformation.Prompt,
                string.Empty,
                0,
                0,
                0,
                0,
                0,
                context.Connection.RemoteIpAddress?.ToString() ?? "",
                requestInformation.StartDate,
                requestInformation.Duration);

            return new AICentralResponse(chatRequestInformation,
                new JsonResultHandler(openAiResponse, chatRequestInformation));
        }
        
    }
}