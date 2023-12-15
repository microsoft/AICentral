using AICentral.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace AICentral.OpenAI.AzureOpenAI;

public class AzureOpenAIEndpointAdapter : OpenAILikeEndpointAdapter
{
    private static readonly string[] HeaderPrefixesToCopy = { "x-", "apim", "operation-location" };
    private readonly string _languageUrl;
    private readonly IEndpointAuthorisationHandler _authHandler;

    public AzureOpenAIEndpointAdapter(
        string id,
        string languageUrl,
        string endpointName,
        Dictionary<string, string> modelMappings,
        IEndpointAuthorisationHandler authHandler) : base(id, languageUrl, endpointName, modelMappings)
    {
        _languageUrl = languageUrl.EndsWith('/') ? languageUrl[..^1] : languageUrl;
        _authHandler = authHandler;
    }

    protected override Task<ResponseMetadata> PreProcess(
        HttpContext context,
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
            openAiResponse.Headers.Contains("operation-location"),
            didHaveTokenLimitHeader ? remainingTokens : null,
            didHaveRequestLimitHeader ? remainingRequests : null));
    }
    
    protected override async Task CustomiseRequest(HttpContext context, AICallInformation aiCallInformation,
        HttpRequestMessage newRequest,
        string? newModelName)
    {
        
        //if there is a model change then set the model on a new outbound JSON request. Else copy the content with no changes
        if (aiCallInformation.IncomingCallDetails.AICallType != AICallType.Other)
        {
            newRequest.Content = await CreateDownstreamResponseWithMappedModelName(aiCallInformation, context.Request, newModelName);
        }
        else
        {
            newRequest.Content = new StreamContent(context.Request.Body);
            newRequest.Content.Headers.Add("Content-Type", context.Request.Headers.ContentType.ToString());
        }

        await _authHandler.ApplyAuthorisationToRequest(context.Request, newRequest);
    }

    /// <summary>
    /// Azure Open AI uses an async pattern for actions like image generation. We need to tweak the operation-location
    /// header else the request to look for the status won't work. 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="openAiResponse"></param>
    /// <returns></returns>
    private Dictionary<string, StringValues> SanitiseHeaders(HttpContext context,
        HttpResponseMessage openAiResponse)
    {
        var proxiedHeaders = new Dictionary<string, StringValues>();
        foreach (var header in openAiResponse.Headers)
        {
            if (HeaderPrefixesToCopy.Any(x => header.Key.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                if (header.Key.Equals("operation-location", StringComparison.InvariantCultureIgnoreCase))
                {
                    proxiedHeaders.Add(header.Key, AdjustAzureOpenAILocationToAICentralHost(context, header));
                }
                else
                {
                    proxiedHeaders.Add(header.Key, new StringValues(header.Value.ToArray()));
                }
            }
        }

        return proxiedHeaders;
    }

    private string AdjustAzureOpenAILocationToAICentralHost(
        HttpContext context,
        KeyValuePair<string, IEnumerable<string>> header)
    {
        var locationRaw = header.Value.Single();
        var location = new Uri(locationRaw);
        var queryParts = QueryHelpers.ParseQuery(location.Query);
        queryParts.Add(AICentralHeaders.AzureOpenAIHostAffinityHeader, EndpointName);

        var builder = new UriBuilder(
            context.Request.Scheme,
            context.Request.Host.Host,
            context.Request.Host.Port ?? 443,
            location.AbsolutePath
        );
        return QueryHelpers.AddQueryString(builder.ToString(), queryParts);
    }

    protected override string BuildUri(
        HttpContext context,
        AICallInformation aiCallInformation,
        string? mappedModelName)
    {
        aiCallInformation.QueryString.TryAdd("api-version", "2023-05-15");

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

        return aiCallInformation.IncomingCallDetails.AICallType == AICallType.Other
            ? QueryHelpers.AddQueryString($"{_languageUrl}{context.Request.Path}", aiCallInformation.QueryString)
            : QueryHelpers.AddQueryString($"{_languageUrl}/openai/deployments/{mappedModelName}/{pathPiece}",
                aiCallInformation.QueryString);
    }
}