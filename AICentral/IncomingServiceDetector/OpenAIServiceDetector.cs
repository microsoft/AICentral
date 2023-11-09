using Newtonsoft.Json.Linq;

namespace AICentral;

public class OpenAIServiceDetector : IAIServiceDetector
{
    public OpenAIServiceDetector(JObject? requestContent, PathString remainingUrlSegments)
    {
        var requestTypeRaw = remainingUrlSegments.ToString().Split('/')[1];

        AICallType = requestTypeRaw switch
        {
            "chat" => AICallType.Chat,
            "embeddings" => AICallType.Embeddings,
            "completions" => AICallType.Completions,
            _ => AICallType.Other
        };

        PromptText = requestContent == null
            ? null
            : AICallType switch
            {
                AICallType.Chat => string.Join(
                    Environment.NewLine,
                    requestContent["messages"]?.Select(x => x.Value<string>("content")) ??
                    Array.Empty<string>()),
                AICallType.Embeddings => requestContent.Value<string>("input") ?? string.Empty,
                AICallType.Completions => string.Join(Environment.NewLine,
                    requestContent["prompt"]?.Select(x => x.Value<string>()) ?? Array.Empty<string>()),
                _ => requestContent.Value<string>("prompt") ?? string.Empty
            };

        IncomingModelName = requestContent?.Value<string>("model");
    }

    public AIServiceType ServiceType => AIServiceType.AzureOpenAI;
    public AICallType AICallType { get; }
    public string? PromptText { get; }
    public string? IncomingModelName { get; }
}