using System.Net.Http.Headers;
using AICentral.Core;

namespace AICentral.Endpoints.OpenAI;

public class OpenAIDownstreamEndpointAdapter : OpenAILikeDownstreamEndpointAdapter
{
    protected override string[] HeadersToIgnore => ["host", "authorization", "api-key"];
    protected override string[] HeaderPrefixesToCopy => ["x-", "openai"];
    internal static readonly Uri OpenAIV1 = new("https://api.openai.com");

    private readonly string? _organization;
    private readonly string _apiKey;

    public OpenAIDownstreamEndpointAdapter(
        string id,
        string endpointName,
        Dictionary<string, string> modelMappings,
        Dictionary<string, string> assistantMappings,
        string apiKey,
        string? organization) : base(id, OpenAIV1, endpointName, modelMappings, assistantMappings)
    {
        _organization = organization;
        _apiKey = apiKey;
    }

    protected override bool IsFixedModelName(AICallType callType, string? callInformationIncomingModelName,
        out string? fixedModelName)
    {
        switch (callType)
        {
            case AICallType.Transcription:
            case AICallType.Translation:
                fixedModelName = "whisper-1";
                return true;
            case AICallType.DALLE2:
                fixedModelName = "dall-e-2";
                return true;
            case AICallType.DALLE3:
                fixedModelName = "dall-e-3";
                return true;
        }

        fixedModelName = null;
        return false;
    }

    protected override string BuildUri(HttpContext context, IncomingCallDetails aiCallInformation, string? incomingAssistantName, string? mappedAssistantName)
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
            AICallType.Files => context.Request.Path.Value!.Replace("/openai/", "/"), //affinity will ensure the request is going to the right place
            AICallType.Threads => context.Request.Path.Value!.Replace("/openai/", "/"), //affinity will ensure the request is going to the right place
            AICallType.Assistants => context.Request.Path.Value!.Replace("/openai/", "/").Replace(incomingAssistantName ?? string.Empty, mappedAssistantName),
            _ => string.Empty
        };

        var requestUri = string.IsNullOrWhiteSpace(pathPiece)
            ? throw new InvalidOperationException(
                "Unable to forward this request from an Azure Open AI request to Open AI")
            : new Uri(OpenAIV1, $"/v1/{pathPiece}");

        return requestUri.AbsoluteUri;
    }

    protected override Task ApplyAuthorisation(HttpContext context, HttpRequestMessage newRequest)
    {
        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        if (!string.IsNullOrWhiteSpace(_organization))
        {
            newRequest.Headers.Add("OpenAI-Organization", _organization);
        }

        return Task.CompletedTask;
    }
}
