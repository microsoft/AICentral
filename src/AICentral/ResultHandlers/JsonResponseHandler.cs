using System.Net;
using System.Text;
using System.Text.Json;
using AICentral.Core;

namespace AICentral.ResultHandlers;

public class JsonResponseHandler: IResponseHandler
{
    private readonly IResponseTransformer? _adapter;

    public JsonResponseHandler(IResponseTransformer? adapter = null)
    {
        _adapter = adapter;
    }
    
    public async Task<AICentralResponse> Handle(
        IRequestContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        DownstreamRequestInformation requestInformation,
        ResponseMetadata responseMetadata
        )
    {
        var response = await JsonDocument.ParseAsync(
            await openAiResponse.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        var responseToReturn = _adapter == null
            ? response
            : openAiResponse.IsSuccessStatusCode
                ? _adapter.Transform(response)
                : response;

        if (openAiResponse.StatusCode == HttpStatusCode.OK)
        {
            var model = response.RootElement.TryGetProperty("model", out var prop) ? prop.GetString() : string.Empty;

            var hasUsage = response.RootElement.TryGetProperty("usage", out var usage);
            var promptTokens = hasUsage
                ? usage.TryGetProperty("prompt_tokens", out var promptTokensProp) ? promptTokensProp.GetInt32() : 0
                : 0;
            var totalTokens = hasUsage
                ? usage.TryGetProperty("total_tokens", out var totalTokensProp) ? totalTokensProp.GetInt32() : 0
                : 0;
            var completionTokens = hasUsage
                ? usage.TryGetProperty("completion_tokens", out var completionTokensProp)
                    ? completionTokensProp.GetInt32()
                    : 0
                : 0;

            var hasResponseContent = response.RootElement.TryGetProperty("choices", out var choicesProp);
            var choices = new Dictionary<int, StringBuilder>();
            if (hasResponseContent)
            {
                foreach (var choiceElement in choicesProp.EnumerateArray())
                {
                    choiceElement.TryGetProperty("index", out var index);
                    var content = choiceElement.TryGetProperty("message", out var messageProp)
                        ? messageProp.TryGetProperty("content", out var contentProp)
                            ? contentProp.GetString() ?? string.Empty
                            : string.Empty
                        : string.Empty;

                    var indexInt = index.GetInt32();
                    if (!choices.ContainsKey(indexInt))
                    {
                        choices.Add(indexInt, new StringBuilder());
                    }

                    choices[indexInt].AppendLine(content);
                }
            }

            var downstreamUsageInformation = new DownstreamUsageInformation(
                requestInformation.LanguageUrl,
                requestInformation.InternalEndpointName,
                model,
                requestInformation.DeploymentName,
                context.GetClientForLoggingPurposes(),
                requestInformation.CallType,
                false,
                requestInformation.Prompt,
                string.Join("\n\n", choices.Select(kvp => $"Choice {kvp.Key}\n\n" + string.Join(string.Empty, kvp.Value))),
                null,
                (promptTokens, completionTokens, totalTokens),
                responseMetadata,
                context.RemoteIpAddress,
                requestInformation.StartDate,
                requestInformation.Duration,
                true);

            return new AICentralResponse(
                downstreamUsageInformation,
                BuildJsonResultHandler(openAiResponse.StatusCode, responseToReturn));
        }

        var chatRequestInformation = new DownstreamUsageInformation(
            requestInformation.LanguageUrl,
            requestInformation.InternalEndpointName,
            null,
            requestInformation.DeploymentName,
            context.UserName ?? string.Empty,
            requestInformation.CallType,
            false,
            requestInformation.Prompt,
            null,
            null,
            null,
            responseMetadata,
            context.RemoteIpAddress?.ToString() ?? "",
            requestInformation.StartDate,
            requestInformation.Duration,
            false);

        return new AICentralResponse(
            chatRequestInformation,
            BuildJsonResultHandler(
                openAiResponse.StatusCode, 
                responseToReturn));
    }

    protected virtual JsonResultHandler BuildJsonResultHandler(HttpStatusCode statusCode, JsonDocument response)
    {
        return new JsonResultHandler(statusCode, response);
    }
}