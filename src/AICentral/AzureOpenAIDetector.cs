using System.Text.Json;
using AICentral.Core;
using Microsoft.AspNetCore.WebUtilities;

namespace AICentral;

public class AzureOpenAIDetector
{
    public async Task<IncomingCallDetails> Detect(string pipelineName, string deploymentName, AICallType callType, HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentType?.Contains("json", StringComparison.InvariantCultureIgnoreCase) ?? false)
        {
            //Pull out the text
            var requestContent = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);

            var promptText = callType switch
            {
                AICallType.Chat => string.Join(
                    '\n',
                    requestContent.RootElement.GetProperty("messages").EnumerateArray().Select(x => x.GetProperty("content").GetString())),
                AICallType.Embeddings => requestContent.RootElement.GetProperty("input").GetString() ?? string.Empty,
                AICallType.DALLE2 => requestContent.RootElement.GetProperty("prompt").GetString() ?? string.Empty,
                AICallType.DALLE3 => requestContent.RootElement.GetProperty("prompt").GetString() ?? string.Empty,
                AICallType.Completions => string.Join('\n', requestContent.RootElement.GetProperty("prompt").EnumerateArray().Select(x => x.GetString())),
                _ => throw new ArgumentOutOfRangeException()
            };

            return new IncomingCallDetails(
                pipelineName,
                callType,
                promptText,
                deploymentName,
                requestContent,
                QueryHelpers.ParseQuery(request.QueryString.Value));
        }

        if (callType == AICallType.Transcription || callType == AICallType.Translation)
        {
            var model = request.Form["model"];
            return new IncomingCallDetails(pipelineName, callType, null, model, null,
                QueryHelpers.ParseQuery(request.QueryString.Value));
        }

        if (request.Method == "GET")
        {
            return new IncomingCallDetails(pipelineName, callType, null, null, null,
                QueryHelpers.ParseQuery(request.QueryString.Value));
        }

        throw new NotSupportedException("Call Type not supported by AI Central");
    }
}