using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral;

public class AzureOpenAiCallInformationExtractor : IIncomingCallExtractor
{
    public async Task<AICallInformation> Extract(HttpRequest request, CancellationToken cancellationToken)
    {
        using var
            requestReader = new StreamReader(request.Body);

        var requestRawContent = await requestReader.ReadToEndAsync(cancellationToken);
        var deserializedRequestContent = (JObject)JsonConvert.DeserializeObject(requestRawContent)!;

        var requestType = AICallType.Other;
        var modelName = string.Empty;
        if (request.Path.StartsWithSegments("/openai/deployments", out var deploymentPath))
        {
            var remaining = deploymentPath.ToString().Split('/');
            modelName = remaining[1];
            requestType = remaining[2] switch
            {
                "chat" => AICallType.Chat,
                "completions" => AICallType.Completions,
                "embeddings" => AICallType.Embeddings,
                _ => AICallType.Other
            };
        }

        var promptText = requestType switch
        {
            AICallType.Chat => string.Join(
                Environment.NewLine,
                deserializedRequestContent["messages"]?.Select(x => x.Value<string>("content")) ??
                Array.Empty<string>()),
            AICallType.Embeddings => deserializedRequestContent.Value<string>("input") ?? string.Empty,
            AICallType.Completions => string.Join(Environment.NewLine,
                deserializedRequestContent["prompt"]?.Select(x => x.Value<string>()) ?? Array.Empty<string>()),
            _ => deserializedRequestContent.Value<string>("prompt")?? String.Empty
        };

        return new AICallInformation(
            AIServiceType.AzureOpenAI,
            requestType,
            modelName,
            deserializedRequestContent,
            promptText,
            QueryHelpers.ParseQuery(request.QueryString.Value ?? string.Empty)
        );
    }
}