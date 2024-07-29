using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using AICentral.Core;
using Microsoft.Extensions.Primitives;
using SharpToken;

namespace AICentral.ResultHandlers;

public class ServerSideEventResponseHandler: IResponseHandler
{
    private static readonly int StreamingLinePrefixLength = "data:".Length;
    private static readonly HashSet<string> EmptySet = new();
    private static readonly ConcurrentDictionary<string, GptEncoding> Encoders = new();

    public async Task<AICentralResponse> Handle(
        IRequestContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        DownstreamRequestInformation requestInformation,
        ResponseMetadata responseMetadata)
    {
        const string activityName = "StreamingResponse";
        using var activity = ActivitySource.AICentralRequestActivitySource.StartActivity(activityName);

        //send the headers down to the client
        context.Response.StatusCode = (int)openAiResponse.StatusCode;

        //squirt the response as it comes in:
        using var openAiResponseReader =
            new StreamReader(await openAiResponse.Content.ReadAsStreamAsync(cancellationToken));
        context.Response.Headers.CacheControl = new StringValues("no-cache");
        context.Response.ContentType = "text/event-stream";

        var model = string.Empty;
        var choices = new Dictionary<int, List<string>>();
        JsonElement? usageProp = null;
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

                    if (lineObject.RootElement.TryGetProperty("usage", out var usagePropTemp))
                    {
                        usageProp = usagePropTemp;
                    }

                    if (requestInformation.CallType == AICallType.Chat)
                    {
                        if (lineObject.RootElement.TryGetProperty("choices", out var choicesProp))
                        {
                            foreach (var choice in choicesProp.EnumerateArray())
                            {
                                if (choice.TryGetProperty("index", out var index))
                                {
                                    if (choice.TryGetProperty("delta", out var deltaProp))
                                    {
                                        if (deltaProp.TryGetProperty("content", out var contentProp))
                                        {
                                            var indexInt = index.GetInt32();
                                            if (!choices.ContainsKey(indexInt))
                                            {
                                                choices.Add(indexInt, new List<string>());
                                            }

                                            choices[indexInt].Add(contentProp.GetString() ?? string.Empty);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (requestInformation.CallType == AICallType.Completions)
                    {
                        if (lineObject.RootElement.TryGetProperty("choices", out var choicesProp))
                        {
                            foreach (var choice in choicesProp.EnumerateArray())
                            {
                                if (choice.TryGetProperty("index", out var index))
                                {
                                    if (choice.TryGetProperty("text", out var textProp))
                                    {
                                        var indexInt = index.GetInt32();
                                        if (!choices.ContainsKey(indexInt))
                                        {
                                            choices.Add(indexInt, new List<string>());
                                        }

                                        choices[indexInt].Add(textProp.GetString() ?? string.Empty);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        //calculate prompt tokens
        var estimatedTokens = new Lazy<(int? EstimatedPromptTokens, int? EstimatedCompletionTokens)>(() =>
        {
            if (!string.IsNullOrWhiteSpace(model))
            {
                var tokeniser = Encoders.GetOrAdd(model, GptEncoding.GetEncodingForModel);
                try
                {
                    int? estimatedPromptTokens = requestInformation.Prompt == null
                        ? 0
                        : tokeniser?.Encode(requestInformation.Prompt, EmptySet, EmptySet).Count ?? 0;

                    var estimatedCompletionTokens = choices.Sum(kvp =>
                        kvp.Value.Sum(x => tokeniser?.Encode(x, EmptySet, EmptySet).Count));

                    return (estimatedPromptTokens, estimatedCompletionTokens);
                }
                catch
                {
                    //not much we can do if we failed to create a Tokeniser
                }
            }

            return (null, null);
        });

        var chatRequestInformation = new DownstreamUsageInformation(
            requestInformation.LanguageUrl,
            requestInformation.InternalEndpointName,
            model,
            requestInformation.DeploymentName,
            context.GetClientForLoggingPurposes(),
            requestInformation.CallType,
            true,
            requestInformation.Prompt,
            string.Join("\n\n", choices.Select(kvp => $"Choice {kvp.Key}\n\n" + string.Join(string.Empty, kvp.Value))),
            estimatedTokens,
            null,
            responseMetadata,
            context.RemoteIpAddress,
            requestInformation.StartDate,
            requestInformation.Duration,
            openAiResponse.IsSuccessStatusCode);

        if (usageProp != null && usageProp.Value.TryGetProperty("prompt_tokens", out var promptTokensElement) &&
            usageProp.Value.TryGetProperty("completion_tokens", out var completionTokensElement))
        {
            var promptTokens = promptTokensElement.GetInt32();
            var completionTokens = completionTokensElement.GetInt32();
            chatRequestInformation = chatRequestInformation with
            {
                EstimatedTokens = null,
                KnownTokens = (promptTokens, completionTokens, promptTokens + completionTokens)
            };
        }
        
        return new AICentralResponse(chatRequestInformation, new StreamAlreadySentResultHandler());
    }
}