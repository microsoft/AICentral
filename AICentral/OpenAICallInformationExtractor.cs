using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral;

public class OpenAICallInformationExtractor : IIncomingCallExtractor
{
    public async Task<AICallInformation> Extract(HttpRequest request, CancellationToken cancellationToken)
    {
        using var
            requestReader = new StreamReader(request.Body);

        var requestRawContent = await requestReader.ReadToEndAsync(cancellationToken);
        var deserializedRequestContent = (JObject)JsonConvert.DeserializeObject(requestRawContent)!;

        request.Path.StartsWithSegments("/v1", out var deploymentPath);
        var requestTypeRaw = deploymentPath.ToString().Split('/')[1];

        var requestType = requestTypeRaw switch
        {
            "chat" => AICallType.Chat,
            "embeddings" => AICallType.Embeddings,
            "completions" => AICallType.Completions,
            _ => AICallType.Other // throw new InvalidOperationException($"AICentral does not currently support {requestTypeRaw}")
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
            _ => deserializedRequestContent.Value<string>("prompt") ?? string.Empty
        };

        var incomingModelName = deserializedRequestContent.Value<string>("model");

        return new AICallInformation(
            AIServiceType.OpenAI,
            requestType,
            incomingModelName,
            deserializedRequestContent,
            promptText,
            QueryHelpers.ParseQuery(request.QueryString.Value ?? string.Empty));
    }
}