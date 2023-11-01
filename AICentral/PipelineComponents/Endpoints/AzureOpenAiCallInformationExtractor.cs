using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json.Linq;

namespace AICentral.PipelineComponents.Endpoints;

public class AzureOpenAiCallInformationExtractor
{
    private static readonly Regex OpenAiUrlRegex = new("^/openai/deployments/(.*?)/(embeddings|chat|completions)(.*?)$");

    public AICallInformation Extract(HttpRequest request, JObject content)
    {
        var openAiUriParts = OpenAiUrlRegex.Match(request.GetEncodedPathAndQuery());

        var requestType = openAiUriParts.Groups[2].Captures[0].Value switch
        {
            "chat" => AICallType.Chat,
            "embeddings" => AICallType.Embeddings,
            "completions" => AICallType.Completions
        };
        
        var promptText = requestType switch
        {
            AICallType.Chat => string.Join(
                Environment.NewLine,
                content["messages"]?.Select(x => x.Value<string>("content")) ?? Array.Empty<string>()),
            AICallType.Embeddings => content.Value<string>("input") ?? string.Empty,
            AICallType.Completions => string.Join(Environment.NewLine, content["prompt"]?.Select(x => x.Value<string>()) ?? Array.Empty<string>())
        };

        var incomingModelName = openAiUriParts.Groups[1].Captures[0].Value;
        return new AICallInformation(requestType, incomingModelName, promptText, $"{openAiUriParts.Groups[2].Captures[0]}{openAiUriParts.Groups[3].Captures[0].Value}");

    }
}