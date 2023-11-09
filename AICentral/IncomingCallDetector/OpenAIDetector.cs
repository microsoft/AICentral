using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral.IncomingServiceDetector;

public class OpenAIDetector : IAIServiceDetector
{
    public bool CanDetect(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/v1", out var remainingPath);
    }

    public async Task<IncomingCallDetails> Detect(HttpRequest request, CancellationToken cancellationToken)
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

        if (aICallType == AICallType.Other)
        {
            return new IncomingCallDetails(AIServiceType.OpenAI, aICallType, null, null, null);
        }

        //Pull out the text
        using var requestReader = new StreamReader(request.Body);
        var requestRawContent = await requestReader.ReadToEndAsync(cancellationToken);
        var requestContent = (JObject)JsonConvert.DeserializeObject(requestRawContent)!;


        var promptText = aICallType switch
        {
            AICallType.Chat => string.Join(
                Environment.NewLine,
                requestContent["messages"]?.Select(x => x.Value<string>("content")) ??
                Array.Empty<string>()),
            AICallType.Embeddings => requestContent.Value<string>("input") ?? string.Empty,
            AICallType.Completions => string.Join(Environment.NewLine,
                requestContent["prompt"]?.Select(x => x.Value<string>()) ?? Array.Empty<string>()),
            _ => throw new ArgumentOutOfRangeException()
        };

        var incomingModelName = requestContent?.Value<string>("model");

        return new IncomingCallDetails(AIServiceType.OpenAI, aICallType, promptText, incomingModelName, requestContent);
    }
}