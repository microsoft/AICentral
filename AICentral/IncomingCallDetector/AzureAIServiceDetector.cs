using Newtonsoft.Json.Linq;

namespace AICentral.IncomingServiceDetector;

public class AzureOpenAIDetector : IAIServiceDetector
{
    public bool CanDetect(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/openai");
    }

    public IncomingCallDetails Detect(HttpRequest request, JObject? requestContent)
    {
        request.Path.StartsWithSegments("/openai", out var remainingUrlSegments);

        var remaining = remainingUrlSegments.ToString().Split('/');
        var callType = remaining[1];
        string? incomingModelName = default;

        if (remaining[1] == "deployments")
        {
            incomingModelName = remaining[2];
            callType = remaining[3];
        }

        var aICallType = callType switch
        {
            "chat" => AICallType.Chat,
            "completions" => AICallType.Completions,
            "embeddings" => AICallType.Embeddings,
            _ => AICallType.Other
        };

        var promptText = requestContent == null
            ? null
            : aICallType switch
            {
                AICallType.Chat => string.Join(
                    Environment.NewLine,
                    requestContent?["messages"]?.Select(x => x.Value<string>("content")) ??
                    Array.Empty<string>()),
                AICallType.Embeddings => requestContent?.Value<string>("input") ?? string.Empty,
                AICallType.Completions => string.Join(Environment.NewLine,
                    requestContent?["prompt"]?.Select(x => x.Value<string>()) ?? Array.Empty<string>()),
                _ => requestContent?.Value<string>("prompt") ?? String.Empty
            };

        return new IncomingCallDetails(AIServiceType.AzureOpenAI, aICallType, promptText, incomingModelName);
    }
}