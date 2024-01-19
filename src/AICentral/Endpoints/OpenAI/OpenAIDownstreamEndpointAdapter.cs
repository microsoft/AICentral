using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.Endpoints.OpenAI;

public class OpenAIDownstreamEndpointAdapter : IDownstreamEndpointAdapter
{
    private static readonly string[] HeadersToIgnore = { "host", "authorization", "api-key" };
    private static readonly string[] HeaderPrefixesToCopy = { "x-", "openai" };
    internal static readonly Uri OpenAIV1 = new("https://api.openai.com");
    private const string OpenAIWellKnownModelNameField = "model";

    private readonly Dictionary<string, string> _modelMappings;
    private readonly string? _organization;
    private readonly string _apiKey;

    public string Id { get; }
    public Uri BaseUrl { get; }
    public string EndpointName { get; }

    public OpenAIDownstreamEndpointAdapter(
        string id,
        string endpointName,
        Dictionary<string, string> modelMappings,
        string apiKey,
        string? organization)
    {
        Id = id;
        EndpointName = endpointName;
        BaseUrl = OpenAIV1;
        _modelMappings = modelMappings;
        _organization = organization;
        _apiKey = apiKey;
    }

    public async Task<Either<AIRequest, IResult>> BuildRequest(IncomingCallDetails callInformation, HttpContext context)
    {
        var incomingModelName = callInformation.IncomingModelName ?? string.Empty;
        _modelMappings.TryGetValue(incomingModelName, out var mappedModelName);

        mappedModelName ??= callInformation.AICallType switch
        {
            AICallType.DALLE2 => "dall-e-2", //Azure Open AI doesn't use a deployment for dall-e-2 requests
            _ => incomingModelName
        };

        if (MappedModelFoundAsEmptyString(callInformation, mappedModelName))
        {
            return new Either<AIRequest, IResult>(Results.NotFound(new { message = "Unknown model mapping" }));
        }

        try
        {
            return new Either<AIRequest, IResult>(
                new AIRequest(await BuildNewRequest(context, callInformation, mappedModelName), mappedModelName));
        }
        catch (InvalidOperationException ie)
        {
            return new Either<AIRequest, IResult>(Results.BadRequest(new { message = ie.Message }));
        }
    }

    /// <summary>
    /// If we can't work out which model this should be then fail the request.
    /// </summary>
    /// <returns></returns>
    private static bool MappedModelFoundAsEmptyString(IncomingCallDetails callInformation, string mappedModelName)
    {
        return callInformation.AICallType != AICallType.Other && mappedModelName == string.Empty;
    }

    private async Task<HttpRequestMessage> BuildNewRequest(HttpContext context, IncomingCallDetails callInformation,
        string? mappedModelName)
    {
        var newRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method),
            BuildUri(context, callInformation, mappedModelName));

        foreach (var header in context.Request.Headers)
        {
            if (HeadersToIgnore.Contains(header.Key.ToLowerInvariant())) continue;

            if (!newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) &&
                newRequest.Content != null)
            {
                newRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        await CustomiseRequest(context, callInformation, newRequest, mappedModelName);

        return newRequest;
    }

    public Task<ResponseMetadata> ExtractResponseMetadata(
        IncomingCallDetails callInformationIncomingCallDetails,
        HttpContext context,
        AIRequest newRequest,
        HttpResponseMessage openAiResponse)
    {
        openAiResponse.Headers.TryGetValues("x-ratelimit-remaining-requests", out var remainingRequestHeaderValues);
        openAiResponse.Headers.TryGetValues("x-ratelimit-remaining-tokens", out var remainingTokensHeaderValues);
        var didHaveRequestLimitHeader =
            long.TryParse(remainingRequestHeaderValues?.FirstOrDefault(), out var remainingRequests);
        var didHaveTokenLimitHeader =
            long.TryParse(remainingTokensHeaderValues?.FirstOrDefault(), out var remainingTokens);

        return Task.FromResult(new ResponseMetadata(
            SanitiseHeaders(context, openAiResponse),
            false,
            didHaveTokenLimitHeader ? remainingTokens : null,
            didHaveRequestLimitHeader ? remainingRequests : null));
    }

    private static Task<HttpContent> CopyResponseWithMappedModelName(
        IncomingCallDetails aiCallInformation,
        HttpRequest incomingRequest,
        string? mappedModelName)
    {
        if (aiCallInformation.AICallType == AICallType.Transcription ||
            aiCallInformation.AICallType == AICallType.Translation)
        {
            return Task.FromResult<HttpContent>(MultipartContentHelper.CopyMultipartContent(incomingRequest,
                mappedModelName, OpenAIWellKnownModelNameField));
        }

        return Task.FromResult<HttpContent>(aiCallInformation.IncomingModelName == mappedModelName
            ? new StringContent(JsonSerializer.Serialize(aiCallInformation.RequestContent),
                Encoding.UTF8,
                "application/json")
            : new StringContent(
                JsonSerializer.Serialize(
                    AddModelName(
                        aiCallInformation.RequestContent!.Deserialize<JsonNode>()!,
                        mappedModelName!)),
                Encoding.UTF8, "application/json"));
    }

    /// <summary>
    /// Adjust the model name to the one used by Open AI
    /// </summary>
    /// <param name="deepClone"></param>
    /// <param name="mappedModelName"></param>
    /// <returns></returns>
    private static JsonNode AddModelName(JsonNode deepClone, string mappedModelName)
    {
        deepClone["model"] = mappedModelName;
        return deepClone;
    }

    private async Task CustomiseRequest(
        HttpContext context,
        IncomingCallDetails aiCallInformation,
        HttpRequestMessage newRequest,
        string? mappedModelName)
    {
        //if there is a model change then set the model on a new outbound JSON request. Else copy the content with no changes
        if (aiCallInformation.AICallType != AICallType.Other)
        {
            newRequest.Content =
                await CopyResponseWithMappedModelName(aiCallInformation, context.Request, mappedModelName);
        }
        else
        {
            newRequest.Content = new StreamContent(context.Request.Body);
            newRequest.Content.Headers.Add("Content-Type", context.Request.Headers.ContentType.ToString());
        }

        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        if (!string.IsNullOrWhiteSpace(_organization))
        {
            newRequest.Headers.Add("OpenAI-Organization", _organization);
        }
    }

    private string BuildUri(HttpContext context, IncomingCallDetails aiCallInformation, string? _)
    {
        var pathPiece = aiCallInformation.AICallType switch
        {
            AICallType.Chat => "chat/completions",
            AICallType.Completions => "completions",
            AICallType.Embeddings => "embeddings",
            AICallType.DALLE2 => "images/generations",
            AICallType.DALLE3 => "images/generations",
            AICallType.Transcription => "audio/transcriptions",
            AICallType.Translation => "audio/translations",
            _ => string.Empty
        };

        var requestUri = string.IsNullOrWhiteSpace(pathPiece)
            ? throw new InvalidOperationException(
                "Unable to forward this request from an Azure Open AI request to Open AI")
            : $"{OpenAIV1}/v1/{pathPiece}";

        return requestUri;
    }

    private Dictionary<string, StringValues> SanitiseHeaders(
        HttpContext context,
        HttpResponseMessage openAiResponse)
    {
        var proxiedHeaders = new Dictionary<string, StringValues>();
        foreach (var header in openAiResponse.Headers)
        {
            if (HeaderPrefixesToCopy.Any(x => header.Key.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                proxiedHeaders.Add(header.Key, new StringValues(header.Value.ToArray()));
            }
        }

        return proxiedHeaders;
    }
}