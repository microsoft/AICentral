using System.Text;
using AICentral.Core;
using AICentral.Endpoints.ResultHandlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpToken;

namespace AICentral.EndpointSelectors;

public static class ServerSideEventResponseHandler
{
    private static readonly int StreamingLinePrefixLength = "data:".Length;

    public static async Task<AICentralResponse> Handle(
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        DownstreamRequestInformation requestInformation)
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
                //Write this out as we read it so we get the fastest response back to the consumer possible. 
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
        var tokeniser = GptEncoding.GetEncodingForModel(model);
        int? estimatedPromptTokens = null;
        int? estimatedCompletionTokens = null;
        var responseText = content.ToString();

        try
        {
            estimatedPromptTokens = requestInformation.Prompt == null
                ? 0
                : tokeniser?.Encode(requestInformation.Prompt, new HashSet<string>(), new HashSet<string>()).Count ?? 0;

            estimatedCompletionTokens = tokeniser?.Encode(responseText, new HashSet<string>(), new HashSet<string>()).Count ?? 0;
        }
        catch
        {
            //not much we can do if we failed to create a Tokeniser (I think they are pulled from the internet)
        }

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