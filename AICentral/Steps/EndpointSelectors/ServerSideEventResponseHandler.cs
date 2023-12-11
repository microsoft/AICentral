using System.Diagnostics;
using System.Text;
using AICentral.Core;
using AICentral.Steps.Endpoints.OpenAILike;
using AICentral.Steps.Endpoints.ResultHandlers;
using Microsoft.DeepDev;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral.Steps.EndpointSelectors;

public class ServerSideEventResponseHandler
{
    private static readonly int StreamingLinePrefixLength = "data:".Length;
    public static async Task<AICentralResponse> Handle(
        Dictionary<string, ITokenizer> tokenisers, 
        HttpContext context,
        CancellationToken cancellationToken, 
        HttpResponseMessage openAiResponse,
        AICentralRequestInformation requestInformation)
    {
        using var activity = AICentralActivitySource.AICentralRequestActivitySource.StartActivity("StreamingResponse");

        //send the headers down to the client
        context.Response.StatusCode = (int)openAiResponse.StatusCode;

        //squirt the response as it comes in:
        using var openAiResponseReader =
            new StreamReader(await openAiResponse.Content.ReadAsStreamAsync(cancellationToken));
        await using var responseWriter = new StreamWriter(context.Response.Body);
        context.Response.ContentType = "text/event-stream";

        var content = new StringBuilder();
        var model = string.Empty;
        while (!openAiResponseReader.EndOfStream)
        {
            var line = await openAiResponseReader.ReadLineAsync(cancellationToken);

            if (line != null)
            {
                await responseWriter.WriteAsync(line);
                await responseWriter.WriteAsync("\n");
                await responseWriter.FlushAsync();

                if (line.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase) &&
                    !line.EndsWith("[done]", StringComparison.InvariantCultureIgnoreCase))
                {
                    var lineObject = (JObject)JsonConvert.DeserializeObject(line.Substring(StreamingLinePrefixLength))!;
                    model = lineObject.Value<string>("model")!;
                    var completions = lineObject["choices"]?.FirstOrDefault()?["delta"]?.Value<string>("content") ?? "";
                    content.Append(completions);
                }
            }
        }

        //calculate prompt tokens
        var tokeniser = tokenisers.TryGetValue(model, out var val) ? val : tokenisers["gpt-35-turbo"];
        var estimatedPromptTokens = requestInformation.Prompt == null ? 0 : tokeniser.Encode(requestInformation.Prompt, Array.Empty<string>()).Count;
        var responseText = content.ToString();
        var estimatedCompletionTokens = tokeniser.Encode(responseText, Array.Empty<string>()).Count;

        var chatRequestInformation = new AICentralUsageInformation(
            requestInformation.LanguageUrl,
            model,
            context.User.Identity?.Name ?? "unknown",
            requestInformation.CallType,
            requestInformation.Prompt,
            responseText,
            estimatedPromptTokens,
            estimatedCompletionTokens,
            0,
            0,
            estimatedPromptTokens + estimatedCompletionTokens,
            context.Connection.RemoteIpAddress?.ToString() ?? "",
            requestInformation.StartDate,
            requestInformation.Duration);

        return new AICentralResponse(chatRequestInformation, new StreamAlreadySentResultHandler());
        
    }
}