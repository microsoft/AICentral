using System.Net;
using System.Security.Claims;
using System.Text;
using AICentral.PipelineComponents.Endpoints.ResultHandlers;
using Microsoft.DeepDev;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral.PipelineComponents.EndpointSelectors;

public abstract class EndpointSelectorBase : IEndpointSelector
{
    private static readonly int StreamingLinePrefixLength = "data:".Length;
    private static readonly string[] HeaderPrefixesToCopy = { "x-", "apim" };

    private static readonly Dictionary<string, ITokenizer> Tokenisers = new()
    {
        ["gpt-35-turbo"] = TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo").Result,
        ["gpt-4"] = TokenizerBuilder.CreateByModelNameAsync("gpt-4").Result,
    };

    /// <param name="lastChanceMustHandle">Used if you have no more servers to try. When this happens we will proxy back whatever response we can.</param>
    protected async Task<AICentralResponse> HandleResponse(
        ILogger logger,
        HttpContext context,
        AICentralRequestInformation requestInformation,
        HttpResponseMessage openAiResponse,
        bool lastChanceMustHandle,
        CancellationToken cancellationToken)
    {
        if (openAiResponse.StatusCode == HttpStatusCode.OK)
        {
            context.Response.Headers.TryAdd("x-aicentral-server", new StringValues(requestInformation.LanguageUrl));
        }
        else
        {
            if (context.Response.Headers.TryGetValue("x-aicentral-failed-servers", out var header))
                context.Response.Headers.Remove("x-aicentral-failed-servers");
            var added = context.Response.Headers.TryAdd("x-aicentral-failed-servers",
                StringValues.Concat(header, requestInformation.LanguageUrl));
        }

        //Now blow up if we didn't succeed
        if (!lastChanceMustHandle)
        {
            openAiResponse.EnsureSuccessStatusCode();
        }

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

        CopyHeaderToResponse(context, openAiResponse);

        if (openAiResponse.StatusCode == HttpStatusCode.OK)
        {
            var model = response.Value<string>("model")!;
            var usage = response["usage"]!;
            var promptTokens = usage.Value<int>("prompt_tokens");
            var totalTokens = usage.Value<int>("total_tokens");
            var completionTokens = usage.Value<int>("completion_tokens");

            var chatRequestInformation = new AICentralUsageInformation(
                requestInformation.LanguageUrl,
                model,
                context.User.Identity?.Name ?? "unknown",
                requestInformation.CallType,
                requestInformation.Prompt,
                0,
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
        else
        {
            var chatRequestInformation = new AICentralUsageInformation(
                requestInformation.LanguageUrl,
                string.Empty,
                context.User.Identity?.Name ?? "unknown",
                requestInformation.CallType,
                requestInformation.Prompt,
                0,
                0,
                0,
                0,
                0,
                context.Connection.RemoteIpAddress?.ToString() ?? "",
                requestInformation.StartDate,
                requestInformation.Duration);

            return new AICentralResponse(chatRequestInformation,
                new JsonResultHandler(openAiResponse, chatRequestInformation));
        }
    }

    private async Task<AICentralResponse> HandleStreamingEndpoint(
        ILogger logger,
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        AICentralRequestInformation requestInformation)
    {
        //send the headers down to the client
        context.Response.StatusCode = (int)openAiResponse.StatusCode;

        CopyHeaderToResponse(context, openAiResponse);

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
                    var completions = lineObject["choices"]?[0]?["delta"]?.Value<string>("content") ?? "";
                    content.AppendLine(completions);
                }
            }
        }

        //calculate prompt tokens
        var estimatedPromptTokens =
            Tokenisers["gpt-35-turbo"].Encode(requestInformation.Prompt, Array.Empty<string>()).Count;
        var estimatedCompletionTokens = Tokenisers[model].Encode(content.ToString(), Array.Empty<string>()).Count;

        logger.LogDebug(
            "Streamed response. Estimated prompt tokens {EstimatedPromptTokens}. Estimated Completion Tokens {EstimatedCompletionTokens}",
            estimatedPromptTokens,
            estimatedCompletionTokens);

        var chatRequestInformation = new AICentralUsageInformation(
            requestInformation.LanguageUrl,
            model,
            context.User.Identity?.Name ?? "unknown",
            requestInformation.CallType,
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

    private static void CopyHeaderToResponse(HttpContext context, HttpResponseMessage openAiResponse)
    {
        foreach (var header in
                 openAiResponse.Headers.Where(x =>
                     HeaderPrefixesToCopy.Any(p => x.Key.StartsWith(p, StringComparison.InvariantCultureIgnoreCase))))
        {
            context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
        }
    }

    public abstract object WriteDebug();

    public abstract Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);
}