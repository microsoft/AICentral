using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using AICentral.PipelineComponents.Endpoints.EndpointAuth;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.DeepDev;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;

namespace AICentral.PipelineComponents.Endpoints.AzureOpenAI;

public class AzureOpenAIEndpointDispatcher : IAICentralEndpointDispatcher
{
    private readonly string _languageUrl;
    private readonly Dictionary<string, string> _modelMappings;
    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly ResiliencePipeline<HttpResponseMessage> _endpointResiliencyStrategy;
    private static readonly int StreamingLinePrefixLength = "data:".Length;
    private static readonly Regex OpenAiUrlRegex = new("^/openai/deployments/(.*?)/(embeddings|chat|completions)(.*?)$");
    
    private static readonly Dictionary<string, ITokenizer> Tokenisers = new()
    {
        ["gpt-35-turbo"] = TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo").Result,
        ["gpt-4"] = TokenizerBuilder.CreateByModelNameAsync("gpt-4").Result,
    };


    public AzureOpenAIEndpointDispatcher(
        string languageUrl, 
        Dictionary<string, string> modelMappings, 
        IEndpointAuthorisationHandler authHandler,
        ResiliencePipeline<HttpResponseMessage> endpointResiliencyStrategy)
    {
        _languageUrl = languageUrl;
        _modelMappings = modelMappings;
        _authHandler = authHandler;
        _endpointResiliencyStrategy = endpointResiliencyStrategy;
    }
    
    public async Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<AzureOpenAIEndpointDispatcherBuilder>>();
        var typedDispatcher = context.RequestServices.GetRequiredService<HttpAIEndpointDispatcher>();

        using var requestReader = new StreamReader(context.Request.Body);
        var requestRawContent = await requestReader.ReadToEndAsync(cancellationToken);
        var deserializedRequestContent = (JObject)JsonConvert.DeserializeObject(requestRawContent)!;
        
        var openAiUriParts = OpenAiUrlRegex.Match(context.Request.GetEncodedPathAndQuery());

        var requestType = openAiUriParts.Groups[2].Captures[0].Value;
        var promptText = requestType switch
        {
            "chat" => string.Join(
                Environment.NewLine,
                deserializedRequestContent["messages"]?.Select(x => x.Value<string>("content")) ?? Array.Empty<string>()),
            "embeddings" => deserializedRequestContent.Value<string>("input"),
            "completions" => deserializedRequestContent.Value<string>("prompt"),
            _ => ""
        };

        var incomingModelName = openAiUriParts.Groups[1].Captures[0].Value;
        
        var mappedModelName = _modelMappings.TryGetValue(incomingModelName, out var mapping)
            ? mapping
            : incomingModelName;

        var newUri = $"{_languageUrl}/openai/deployments/{mappedModelName}/{openAiUriParts.Groups[2].Captures[0]}{openAiUriParts.Groups[3].Captures[0].Value}";
        logger.LogDebug("Rewritten URL from {OriginalUrl} to {NewUrl}. Incoming Model: {IncomingModelName}. Mapped Model: {MappedModelName}", 
            context.Request.GetEncodedUrl(), 
            newUri,
            incomingModelName,
            mappedModelName);

        var now = DateTimeOffset.Now;
        var sw = new Stopwatch();
        sw.Start();
        var openAiResponse = await typedDispatcher.Dispatch(context, _endpointResiliencyStrategy, newUri, requestRawContent, _authHandler, cancellationToken);
        sw.Stop();

        //decision point... If this is a streaming request, then we should start streaming the result now.
        logger.LogDebug("Received Azure Open AI Response. Status Code: {StatusCode}", openAiResponse.StatusCode);

        //decision point... If this is a streaming request, then we should start streaming the result now.
        if (openAiResponse.Headers.TransferEncodingChunked == true)
        {
            logger.LogDebug("Detected chunked encoding response. Streaming response back to consumer");
            return await HandleStreamingEndpoint(logger, context, cancellationToken, openAiResponse, now, sw, promptText);
        }
        else
        {
            logger.LogDebug("Detected non-chunked encoding response. Sending response back to consumer");
            return await HandleSynchronousEndpoint(logger, context, cancellationToken, openAiResponse, now, sw, promptText);
        }
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "AzureOpenAI",
            Url = _languageUrl,
            ModelMappings = _modelMappings,
            Auth = _authHandler.WriteDebug()
        };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

    private async Task<AICentralResponse> HandleSynchronousEndpoint(
        ILogger<AzureOpenAIEndpointDispatcherBuilder> logger,
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        DateTimeOffset now,
        Stopwatch sw,
        string promptText)
    {
        var rawResponse = await openAiResponse.Content.ReadAsStringAsync(cancellationToken);
        var response = (JObject)JsonConvert.DeserializeObject(rawResponse)!;
        
        var chatRequestInformation = new AICentralUsageInformation(
            _languageUrl,
            promptText,
            0,
            0,
            0,
            0,
            0,
            context.Connection.RemoteIpAddress?.ToString() ?? "",
            now,
            sw.Elapsed);

        if (openAiResponse.StatusCode == HttpStatusCode.OK)
        {
            var usage = response["usage"]!;
            var promptTokens = usage.Value<int>("prompt_tokens");
            var totalTokens = usage.Value<int>("total_tokens");
            var completionTokens = usage.Value<int>("completion_tokens");

            //calculate prompt tokens
            var estimatedPromptTokens = Tokenisers["gpt-35-turbo"].Encode(promptText, Array.Empty<string>()).Count;

            logger.LogDebug(
                "Full response. Estimated prompt tokens {EstimatedPromptTokens}. Actual {ActualPromptTokens}",
                estimatedPromptTokens, promptTokens);
            
            chatRequestInformation = new AICentralUsageInformation(
                _languageUrl,
                promptText,
                estimatedPromptTokens,
                0,
                promptTokens,
                completionTokens,
                totalTokens,
                context.Connection.RemoteIpAddress?.ToString() ?? "",
                now,
                sw.Elapsed);

        }

        return new AICentralResponse(
            chatRequestInformation,
            new JsonResultHandler(openAiResponse, chatRequestInformation));
    }

    private async Task<AICentralResponse> HandleStreamingEndpoint(ILogger<AzureOpenAIEndpointDispatcherBuilder> logger,
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        DateTimeOffset now,
        Stopwatch sw,
        string promptText)
    {
        //calculate prompt tokens
        var estimatedPromptTokens = Tokenisers["gpt-35-turbo"].Encode(promptText, Array.Empty<string>()).Count;
        
        //send the headers down to the client
        context.Response.StatusCode = (int)openAiResponse.StatusCode;
        foreach (var header in openAiResponse.Headers)
        {
            context.Response.Headers.Add(header.Key, header.Value.ToArray());
        }

        //squirt the response as it comes in:
        using var openAiResponseReader = new StreamReader(await openAiResponse.Content.ReadAsStreamAsync(cancellationToken)); 
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
            _languageUrl,
            promptText,
            estimatedPromptTokens,
            estimatedCompletionTokens,
            0,
            0,
            estimatedPromptTokens + estimatedCompletionTokens,
            context.Connection.RemoteIpAddress?.ToString() ?? "",
            now,
            sw.Elapsed);
        
        return new AICentralResponse(chatRequestInformation, new StreamingResultHandler());
    }
}