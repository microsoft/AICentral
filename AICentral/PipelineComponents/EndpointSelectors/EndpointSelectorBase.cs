using System.Net;
using AICentral.PipelineComponents.Endpoints.ResultHandlers;
using Microsoft.DeepDev;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral.PipelineComponents.EndpointSelectors;

public abstract class EndpointSelectorBase: IEndpointSelector
{
    private static readonly int StreamingLinePrefixLength = "data:".Length;

    private static readonly Dictionary<string, ITokenizer> Tokenisers = new()
    {
        ["gpt-35-turbo"] = TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo").Result,
        ["gpt-4"] = TokenizerBuilder.CreateByModelNameAsync("gpt-4").Result,
    };

    protected async Task<AICentralResponse> HandleResponse(
        ILogger logger,
        HttpContext context,
        AICentralRequestInformation requestInformation,
        HttpResponseMessage openAiResponse,
        CancellationToken cancellationToken)
    {
        if (openAiResponse.StatusCode == HttpStatusCode.OK)
        {
            context.Response.Headers.TryAdd("x-aicentral-server", new StringValues(requestInformation.LanguageUrl));
        }
        else
        {
            context.Response.Headers.TryAdd("x-aicentral-failed-servers", new StringValues(requestInformation.LanguageUrl));
        }

        //Now blow up if we didn't succeed
        openAiResponse.EnsureSuccessStatusCode();

        if (openAiResponse.Headers.TransferEncodingChunked == true)
        {
            logger.LogDebug("Detected chunked encoding response. Streaming response back to consumer");
            return await HandleStreamingEndpoint(
                logger,
                context,
                cancellationToken,
                openAiResponse,
                requestInformation);
        }

        logger.LogDebug("Detected non-chunked encoding response. Sending response back to consumer");
        return await HandleSynchronousEndpoint(
            logger,
            context,
            cancellationToken,
            openAiResponse,
            requestInformation);
    }

    private async Task<AICentralResponse> HandleSynchronousEndpoint(
        ILogger logger,
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        AICentralRequestInformation requestInformation)
    {
        var rawResponse = await openAiResponse.Content.ReadAsStringAsync(cancellationToken);
        var response = (JObject)JsonConvert.DeserializeObject(rawResponse)!;

        var usage = response["usage"]!;
        var promptTokens = usage.Value<int>("prompt_tokens");
        var totalTokens = usage.Value<int>("total_tokens");
        var completionTokens = usage.Value<int>("completion_tokens");

        //calculate prompt tokens
        var estimatedPromptTokens =
            Tokenisers["gpt-35-turbo"].Encode(requestInformation.Prompt, Array.Empty<string>()).Count;

        logger.LogDebug(
            "Full response. Estimated prompt tokens {EstimatedPromptTokens}. Actual {ActualPromptTokens}",
            estimatedPromptTokens, promptTokens);

        var chatRequestInformation = new AICentralUsageInformation(
            requestInformation.LanguageUrl,
            requestInformation.Prompt,
            estimatedPromptTokens,
            0,
            promptTokens,
            completionTokens,
            totalTokens,
            context.Connection.RemoteIpAddress?.ToString() ?? "",
            requestInformation.StartDate,
            requestInformation.Duration);

        return new AICentralResponse(
            chatRequestInformation,
            new JsonResultHandler(openAiResponse, chatRequestInformation));
    }

    private async Task<AICentralResponse> HandleStreamingEndpoint(
        ILogger logger,
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        AICentralRequestInformation requestInformation)
    {
        //calculate prompt tokens
        var estimatedPromptTokens =
            Tokenisers["gpt-35-turbo"].Encode(requestInformation.Prompt, Array.Empty<string>()).Count;

        //send the headers down to the client
        context.Response.StatusCode = (int)openAiResponse.StatusCode;
        foreach (var header in openAiResponse.Headers)
        {
            context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
        }

        //squirt the response as it comes in:
        using var openAiResponseReader =
            new StreamReader(await openAiResponse.Content.ReadAsStreamAsync(cancellationToken));
        await using var writer = new StreamWriter(context.Response.Body);
        var estimatedCompletionTokens = 0;
        while (!openAiResponseReader.EndOfStream)
        {
            var line = (await openAiResponseReader.ReadLineAsync(cancellationToken))!;

            await writer.WriteLineAsync(line);
            await writer.FlushAsync();

            if (line.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase) &&
                !line.EndsWith("[done]", StringComparison.InvariantCultureIgnoreCase))
            {
                var lineObject = (JObject)JsonConvert.DeserializeObject(line.Substring(StreamingLinePrefixLength))!;
                var model = lineObject.Value<string>("model")!;
                var completions = lineObject["choices"]?[0]?["delta"]?.Value<string>("content") ?? "";
                estimatedCompletionTokens += Tokenisers[model].Encode(completions, Array.Empty<string>()).Count;
            }
        }

        logger.LogDebug(
            "Streamed response. Estimated prompt tokens {EstimatedPromptTokens}. Estimated Completion Tokens {EstimatedCompletionTokens}",
            estimatedPromptTokens,
            estimatedCompletionTokens);

        var chatRequestInformation = new AICentralUsageInformation(
            requestInformation.LanguageUrl,
            requestInformation.Prompt,
            estimatedPromptTokens,
            estimatedCompletionTokens,
            0,
            0,
            estimatedPromptTokens + estimatedCompletionTokens,
            context.Connection.RemoteIpAddress?.ToString() ?? "",
            requestInformation.StartDate,
            requestInformation.Duration);

        return new AICentralResponse(chatRequestInformation, new StreamingResultHandler());
    }

    public abstract object WriteDebug();

    public abstract Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);
}