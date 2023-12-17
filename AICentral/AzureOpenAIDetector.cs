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
        var callType = remaining[1];
        string? incomingModelName = default;

        if (remaining[1] == "deployments")
        {
            incomingModelName = remaining[2];
            callType = string.Join('/', remaining[3..]);
        }

        var aICallType = callType switch
        {
            "chat/completions" => AICallType.Chat,
            "completions" => AICallType.Completions,
            "embeddings" => AICallType.Embeddings,
            "images/generations" => remaining[1] == "deployments" ? AICallType.DALLE3 : AICallType.Other,
            "audio/transcriptions" => AICallType.Transcription,
            "audio/translations" => AICallType.Translation,
            _ => AICallType.Other
        };

        if (request.ContentType == "application/json")
        {
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
                AICallType.DALLE3 => requestContent.Value<string>("prompt") ?? string.Empty,
                AICallType.Completions => string.Join(Environment.NewLine,
                    requestContent["prompt"]?.Select(x => x.Value<string>()) ?? Array.Empty<string>()),
                _ => throw new ArgumentOutOfRangeException()
            };

            
            return new IncomingCallDetails(
                aICallType,
                promptText,
                incomingModelName,
                requestContent,
                QueryHelpers.ParseQuery(request.QueryString.Value));
        }

        request.EnableBuffering(); //We are going to read the stream - might need to re-read it later
        return new IncomingCallDetails(aICallType, null, null, null, QueryHelpers.ParseQuery(request.QueryString.Value));

    }
}