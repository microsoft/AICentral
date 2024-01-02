using System.Net;
using System.Text.Json;
using AICentral.Core;

namespace AICentral.EndpointSelectors.ResultHandlers;

public static class JsonResponseHandler
{
    public static async Task<AICentralResponse> Handle(
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        DownstreamRequestInformation requestInformation)
    {
        var response = await JsonDocument.ParseAsync(
            await openAiResponse.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        if (openAiResponse.StatusCode == HttpStatusCode.OK)
        {
            var model = response.RootElement.TryGetProperty("model", out var prop) ? prop.GetString() : string.Empty;
            
            var hasUsage = response.RootElement.TryGetProperty("usage", out var usage);
            var promptTokens = hasUsage ? usage.TryGetProperty("prompt_tokens", out var promptTokensProp) ? promptTokensProp.GetInt32() : 0 : 0;
            var totalTokens = hasUsage ? usage.TryGetProperty("total_tokens", out var totalTokensProp) ? totalTokensProp.GetInt32() : 0 : 0;
            var completionTokens = hasUsage ? usage.TryGetProperty("completion_tokens", out var completionTokensProp) ? completionTokensProp.GetInt32() : 0 : 0;
            
            var responseContent = response.RootElement.TryGetProperty("choices", out var choicesProp)
                ? choicesProp.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var messageProp)
                    ? messageProp.TryGetProperty("content", out var contentProp)
                        ? contentProp.GetString()
                        : string.Empty
                    : string.Empty
                : string.Empty;

            var chatRequestInformation = new DownstreamUsageInformation(
                requestInformation.LanguageUrl,
                model,
                context.User.Identity?.Name ?? "unknown",
                requestInformation.CallType,
                requestInformation.Prompt,
                responseContent,
                null,
                null,
                promptTokens,
                completionTokens,
                totalTokens,
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                requestInformation.StartDate,
                requestInformation.Duration);

            return new AICentralResponse(
                chatRequestInformation,
                new JsonResultHandler(openAiResponse, response));
        }
        else
        {
            var chatRequestInformation = new DownstreamUsageInformation(
                requestInformation.LanguageUrl,
                null,
                context.User.Identity?.Name ?? "unknown",
                requestInformation.CallType,
                requestInformation.Prompt,
                null,
                null,
                null,
                null,
                null,
                null,
                context.Connection.RemoteIpAddress?.ToString() ?? "",
                requestInformation.StartDate,
                requestInformation.Duration);

            return new AICentralResponse(chatRequestInformation,
                new JsonResultHandler(openAiResponse, response));
        }
    }
}