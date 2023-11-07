using System.Text.RegularExpressions;
using AICentral.PipelineComponents.Endpoints;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral;

public class AzureOpenAiCallInformationExtractor : IIncomingCallExtractor
{
    private static readonly Regex
        OpenAiUrlRegex = new("^/openai/deployments/(.*?)/(embeddings|chat|completions|images)(.*?)$");

    public async Task<AICallInformation> Extract(HttpRequest request, CancellationToken cancellationToken)
    {
        using var
            requestReader = new StreamReader(request.Body);

        var requestRawContent = await requestReader.ReadToEndAsync(cancellationToken);
        var deserializedRequestContent = (JObject)JsonConvert.DeserializeObject(requestRawContent)!;

        var openAiUriParts = OpenAiUrlRegex.Match(request.GetEncodedPathAndQuery());
        var requestTypeRaw = openAiUriParts.Groups[2].Captures[0].Value;

        var requestType = requestTypeRaw switch
        {
            "chat" => AICallType.Chat,
            "embeddings" => AICallType.Embeddings,
            "completions" => AICallType.Completions,
            "images" => AICallType.Images,
            _ => throw new InvalidOperationException($"AICentral does not currently support {requestTypeRaw}")
        };

        var promptText = requestType switch
        {
            AICallType.Chat => string.Join(
                Environment.NewLine,
                deserializedRequestContent["messages"]?.Select(x => x.Value<string>("content")) ??
                Array.Empty<string>()),
            AICallType.Embeddings => deserializedRequestContent.Value<string>("input") ?? string.Empty,
            AICallType.Completions => string.Join(Environment.NewLine,
                deserializedRequestContent["prompt"]?.Select(x => x.Value<string>()) ?? Array.Empty<string>()),
            AICallType.Images => string.Join(Environment.NewLine,
                deserializedRequestContent["prompt"]?.Value<string>() ?? string.Empty),
            _ => throw new InvalidOperationException($"Unknown AICallType")
        };

        var incomingModelName = openAiUriParts.Groups[1].Captures[0].Value;
        return new AICallInformation(
            requestType,
            incomingModelName,
            deserializedRequestContent,
            promptText,
            $"{openAiUriParts.Groups[2].Captures[0]}{openAiUriParts.Groups[3].Captures[0].Value}");
    }
}