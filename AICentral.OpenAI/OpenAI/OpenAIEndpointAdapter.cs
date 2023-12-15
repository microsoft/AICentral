using System.Net.Http.Headers;
using AICentral.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AICentral.OpenAI.OpenAI;

public class OpenAIEndpointAdapter : OpenAILikeEndpointAdapter
{
    internal const string OpenAIV1 = "https://api.openai.com";
    private readonly string? _organization;
    private readonly string _apiKey;

    public OpenAIEndpointAdapter(string id,
        string endpointName,
        Dictionary<string, string> modelMappings,
        string apiKey,
        string? organization) : base(id, OpenAIV1, endpointName, modelMappings)
    {
        _organization = organization;
        _apiKey = apiKey;
    }

    protected override Task<ResponseMetadata> PreProcess(HttpContext context,
        AIRequest downstreamRequest,
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

    protected override async Task CustomiseRequest(
        HttpContext context,
        AICallInformation aiCallInformation,
        HttpRequestMessage newRequest,
        string? mappedModelName)
    {
        //if there is a model change then set the model on a new outbound JSON request. Else copy the content with no changes
        if (aiCallInformation.IncomingCallDetails.AICallType != AICallType.Other)
        {
            newRequest.Content = await CreateDownstreamResponseWithMappedModelName(aiCallInformation, context.Request, mappedModelName);
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

    protected override string BuildUri(HttpContext context, AICallInformation aiCallInformation, string? _)
    {
        var pathPiece = aiCallInformation.IncomingCallDetails.AICallType switch
        {
            AICallType.Chat => "chat/completions",
            AICallType.Completions => "completions",
            AICallType.Embeddings => "embeddings",
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
        return openAiResponse.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray()));
    }
}