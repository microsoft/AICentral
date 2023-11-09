using Newtonsoft.Json.Linq;

namespace AICentral.IncomingServiceDetector;

public class OpenAIServiceDetector : IAIServiceDetector
{
    public bool CanDetect(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/v1", out var remainingPath);
    }

    public IncomingCallDetails Detect(HttpRequest request, JObject? requestContent)
    {
        request.Path.StartsWithSegments("/v1", out var remainingUrlSegments);
        var requestTypeRaw = remainingUrlSegments.ToString().Split('/')[1];

        var aICallType = requestTypeRaw switch
        {
            "chat" => AICallType.Chat,
            "embeddings" => AICallType.Embeddings,
            "completions" => AICallType.Completions,
            _ => AICallType.Other
        };

        var promptText = requestContent == null
            ? null
            : aICallType switch
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

        var incomingModelName = requestContent?.Value<string>("model");
        return new IncomingCallDetails(AIServiceType.AzureOpenAI, aICallType, promptText, incomingModelName);
        
    }
}