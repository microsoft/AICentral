using Newtonsoft.Json.Linq;

namespace AICentral;

public class AzureOpenAIServiceDetector : IAIServiceDetector
{
    public AzureOpenAIServiceDetector(HttpRequest request, JObject? requestContent, PathString remainingUrlSegments)
    {
        var remaining = remainingUrlSegments.ToString().Split('/');
        var callType = remaining[1];
        if (remaining[1] == "deployments")
        {
            IncomingModelName = remaining[2];
            callType = remaining[3];
        }

        AICallType = callType switch
        {
            "chat" => AICallType.Chat,
            "completions" => AICallType.Completions,
            "embeddings" => AICallType.Embeddings,
            _ => AICallType.Other
        };

        PromptText = requestContent == null
            ? null
            : AICallType switch
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
    }

    public AIServiceType ServiceType => AIServiceType.AzureOpenAI;
    public AICallType AICallType { get; }
    public string? PromptText { get; }
    public string? IncomingModelName { get; }
}