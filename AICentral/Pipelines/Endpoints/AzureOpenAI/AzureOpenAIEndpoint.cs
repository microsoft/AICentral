using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using AICentral.Pipelines.Endpoints.EndpointAuth;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.DeepDev;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace AICentral.Pipelines.Endpoints.AzureOpenAI;

public class AzureOpenAIEndpoint : IAICentralEndpoint, IAICentralEndpointRuntime
{
    private readonly string _languageUrl;
    private readonly string _modelName;
    private readonly ResiliencePipeline<HttpResponseMessage> _retry;
    private static readonly int StreamingLinePrefixLength = "data:".Length;
    private static readonly HttpStatusCode[] StatusCodesToRetry = { HttpStatusCode.TooManyRequests };
    private static readonly Regex OpenAiUrlRegex = new Regex("^/openai/deployments/(.*?)/(.*?)$");


    private static readonly Dictionary<string, ITokenizer> Tokenisers = new()
    {
        ["gpt-35-turbo"] = TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo").Result,
        ["gpt-4"] = TokenizerBuilder.CreateByModelNameAsync("gpt-4").Result,
    };

    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly string _clientName;

    public AzureOpenAIEndpoint(
        string languageUrl,
        string modelName,
        AzureOpenAIAuthenticationType authenticationType,
        string? authenticationKey)
    {
        _clientName = Guid.NewGuid().ToString();
        _languageUrl = languageUrl;
        _modelName = modelName;

        _authHandler = authenticationType switch
        {
            AzureOpenAIAuthenticationType.ApiKey => new KeyAuth(authenticationKey ??
                                                                throw new ArgumentException(
                                                                    "Missing api-key for Authrntication Type")),
            AzureOpenAIAuthenticationType.Entra => new EntraAuth(),
            AzureOpenAIAuthenticationType.EntraPassThrough => new BearerTokenPassThrough(),
            _ => throw new ArgumentOutOfRangeException(nameof(authenticationType), authenticationType, null)
        };

        var handler = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>(e =>
                e.StatusCode.HasValue && StatusCodesToRetry.Contains(e.StatusCode.Value));

        _retry = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = handler
            })
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                Delay = TimeSpan.FromSeconds(0.2),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = 3,
                ShouldHandle = handler
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAIEndpointDispatcher, AIEndpointDispatcher>();
        services.AddHttpClient<HttpAIEndpointDispatcher>();
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

    public static string ConfigName => "AzureOpenAIEndpoint";

    public static IAICentralEndpoint BuildFromConfig(Dictionary<string, string> parameters)
    {
        return new AzureOpenAIEndpoint(
            parameters["LanguageEndpoint"],
            parameters["ModelName"],
            Enum.Parse<AzureOpenAIAuthenticationType>(parameters["AuthenticationType"]),
            parameters.TryGetValue("ApiKey", out var value) ? value : string.Empty);
    }

    public async Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<AzureOpenAIEndpoint>>();
        var typedDispatcher = context.RequestServices.GetRequiredService<HttpAIEndpointDispatcher>();

        using var requestReader = new StreamReader(context.Request.Body);
        var requestRawContent = await requestReader.ReadToEndAsync(cancellationToken);
        var deserializedRequestContent = (JObject)JsonConvert.DeserializeObject(requestRawContent)!;
        var promptText = string.Join(
            Environment.NewLine,
            deserializedRequestContent["messages"]?.Select(x => x.Value<string>("content")) ?? Array.Empty<string>());

        var openAiUriParts = OpenAiUrlRegex.Match(context.Request.GetEncodedPathAndQuery());
        var newUri = $"{_languageUrl}/openai/deployments/{_modelName}/{openAiUriParts.Groups[2].Captures[0].Value}";
        logger.LogDebug("Rewritten URL from {OriginalUrl} to {NewUrl}", context.Request.GetEncodedUrl(), newUri);

        var now = DateTimeOffset.Now;
        var sw = new Stopwatch();
        sw.Start();
        var openAiResponse = await typedDispatcher.Dispatch(context, _retry, newUri, requestRawContent, _authHandler, cancellationToken);
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
            Model = _modelName,
            Auth = _authHandler.WriteDebug()
        };
    }

    private async Task<AICentralResponse> HandleSynchronousEndpoint(
        ILogger<AzureOpenAIEndpoint> logger,
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        DateTimeOffset now,
        Stopwatch sw,
        string promptText)
    {
        var rawResponse = await openAiResponse.Content.ReadAsStringAsync(cancellationToken);
        var response = (JObject)JsonConvert.DeserializeObject(rawResponse)!;
        var usage = response["usage"]!;
        var promptTokens = usage.Value<int>("prompt_tokens");
        var totalTokens = usage.Value<int>("total_tokens");
        var completionTokens = usage.Value<int>("completion_tokens");

        //calculate prompt tokens
        var estimatedPromptTokens = Tokenisers["gpt-35-turbo"].Encode(promptText, Array.Empty<string>()).Count;

        logger.LogDebug("Full response. Estimated prompt tokens {EstimatedPromptTokens}. Actual {ActualPromptTokens}",
            estimatedPromptTokens, promptTokens);

        var chatRequestInformation = new AICentralUsageInformation(
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

        return new AICentralResponse(
            chatRequestInformation,
            new AzureOpenAIActionResultHandler(openAiResponse, chatRequestInformation));
    }

    private async Task<AICentralResponse> HandleStreamingEndpoint(ILogger<AzureOpenAIEndpoint> logger,
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
        
        return new AICentralResponse(chatRequestInformation, new AzureOpenAIActionStreamingResultHandler());
    }

    public IAICentralEndpointRuntime Build()
    {
        return this;
    }
}