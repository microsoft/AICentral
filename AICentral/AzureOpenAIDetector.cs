using AICentral.Core;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral;

public class AzureOpenAIDetector
{
    public bool CanDetect(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/openai");
    }

    public async Task<IncomingCallDetails> Detect(HttpRequest request, CancellationToken cancellationToken)
    {
        request.Path.StartsWithSegments("/openai", out var remainingUrlSegments);

        var remaining = remainingUrlSegments.ToString().Split('/');
        var callTypeFromUrl = remaining[1];
        string? incomingModelName = default;

        if (remaining[1] == "deployments")
        {
            incomingModelName = remaining[2];
            callTypeFromUrl = string.Join('/', remaining[3..]);
        }

        var callType = callTypeFromUrl switch
        {
            "chat/completions" => AICallType.Chat,
            "completions" => AICallType.Completions,
            "embeddings" => AICallType.Embeddings,
            "images" => AICallType.DALLE2,
            "operations" => AICallType.Other,
            "images/generations" => AICallType.DALLE3,
            "audio/transcriptions" => AICallType.Transcription,
            "audio/translations" => AICallType.Translation,
            _ => AICallType.Other
        };

        if (request.ContentType?.Contains("json", StringComparison.InvariantCultureIgnoreCase) ?? false)
        {
            //Pull out the text
            using var requestReader = new StreamReader(request.Body);
            var requestRawContent = await requestReader.ReadToEndAsync(cancellationToken);
            var requestContent = (JObject)JsonConvert.DeserializeObject(requestRawContent)!;

            var promptText = callType switch
            {
                AICallType.Chat => string.Join(
                    '\n',
                    requestContent["messages"]?.Select(x => x.Value<string>("content")) ??
                    Array.Empty<string>()),
                AICallType.Embeddings => requestContent.Value<string>("input") ?? string.Empty,
                AICallType.DALLE2 => requestContent.Value<string>("prompt") ?? string.Empty,
                AICallType.DALLE3 => requestContent.Value<string>("prompt") ?? string.Empty,
                AICallType.Completions => string.Join('\n',
                    requestContent["prompt"]?.Select(x => x.Value<string>()) ?? Array.Empty<string>()),
                _ => throw new ArgumentOutOfRangeException()
            };

            return new IncomingCallDetails(
                callType,
                promptText,
                incomingModelName,
                requestContent,
                QueryHelpers.ParseQuery(request.QueryString.Value));
        }

        if (callType == AICallType.Transcription || callType == AICallType.Translation)
        {
            var model = request.Form["model"];
            return new IncomingCallDetails(callType, null, model, null,
                QueryHelpers.ParseQuery(request.QueryString.Value));
        }

        if (request.Method == "GET")
        {
            return new IncomingCallDetails(callType, null, null, null,
                QueryHelpers.ParseQuery(request.QueryString.Value));
        }

        throw new NotSupportedException("Call Type not supported by AI Central");
    }
}