using System.Collections.Concurrent;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using AICentral.Core;
using Microsoft.Extensions.Primitives;
using SharpToken;

namespace AICentral.EndpointSelectors.ResultHandlers;

public static class ServerSideEventResponseHandler
{
    private static readonly int StreamingLinePrefixLength = "data:".Length;
    private static readonly HashSet<string> EmptySet = new();
    private static readonly ConcurrentDictionary<string, GptEncoding> Encoders = new();


    public static async Task<AICentralResponse> Handle(
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        DownstreamRequestInformation requestInformation)
    {
        const string activityName = "StreamingResponse";
        using var activity = AICentralActivitySource.AICentralRequestActivitySource.StartActivity(activityName);

        //send the headers down to the client
        context.Response.StatusCode = (int)openAiResponse.StatusCode;

        //squirt the response as it comes in:
        using var openAiResponseReader = new StreamReader(await openAiResponse.Content.ReadAsStreamAsync(cancellationToken));
        context.Response.Headers.CacheControl = new StringValues("no-cache");
        context.Response.ContentType = "text/event-stream";

        var content = new List<string>();
        var model = string.Empty;
        while (!openAiResponseReader.EndOfStream)
        {
            var line = await openAiResponseReader.ReadLineAsync(cancellationToken);

            if (line != null)
            {
                //Write this out as we read it so we get the fastest response back to the consumer possible. 
                await context.Response.WriteAsync($"{line}\n", cancellationToken);

                if (line.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase) &&
                    !line.EndsWith("[done]", StringComparison.InvariantCultureIgnoreCase))
                {
                    var lineObject = JsonDocument.Parse(line.Substring(StreamingLinePrefixLength));
                    model = lineObject.RootElement.GetProperty("model").GetString();

                    if (lineObject.RootElement.TryGetProperty("choices", out var choicesProp))
                    {
                        if (choicesProp.GetArrayLength() > 0)
                        {
                            if (choicesProp[0].TryGetProperty("delta", out var deltaProp))
                            {
                                if (deltaProp.TryGetProperty("content", out var contentProp))
                                {
                                    content.Add(contentProp.GetString() ?? string.Empty);
                                }
                            }
                        }
                    }
                }
            }
        }

        //calculate prompt tokens
        int? estimatedPromptTokens = null;
        int? estimatedCompletionTokens = null;
        if (!string.IsNullOrWhiteSpace(model))
        {
            var tokeniser = Encoders.GetOrAdd(model, GptEncoding.GetEncodingForModel);
            try
            {
                estimatedPromptTokens = requestInformation.Prompt == null
                    ? 0
                    : tokeniser?.Encode(requestInformation.Prompt, EmptySet, EmptySet).Count ?? 0;

                estimatedCompletionTokens = content.Sum(x => tokeniser?.Encode(x, EmptySet, EmptySet).Count);
            }
            catch
            {
                //not much we can do if we failed to create a Tokeniser (I think they are pulled from the internet)
            }
        }

        var chatRequestInformation = new DownstreamUsageInformation(
            requestInformation.LanguageUrl,
            model,
            context.User.Identity?.Name ?? "unknown",
            requestInformation.CallType,
            requestInformation.Prompt,
            string.Join("", content),
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