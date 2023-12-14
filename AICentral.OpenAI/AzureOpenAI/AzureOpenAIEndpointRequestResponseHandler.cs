using System.Text;
using AICentral.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral.OpenAI.AzureOpenAI;

public class AzureOpenAIEndpointRequestResponseHandler : OpenAILikeEndpointRequestResponseHandler
{
    private static readonly string[] HeaderPrefixesToCopy = { "x-", "apim", "operation-location" };
    private readonly string _languageUrl;
    private readonly IEndpointAuthorisationHandler _authHandler;

    public AzureOpenAIEndpointRequestResponseHandler(
        string id,
        string languageUrl,
        string endpointName,
        Dictionary<string, string> modelMappings,
        IEndpointAuthorisationHandler authHandler) : base(id, languageUrl, endpointName, modelMappings)
    {
        _languageUrl = languageUrl.EndsWith('/') ? languageUrl[..^1] : languageUrl;
        _authHandler = authHandler;
    }

    protected override Task ExtractDiagnostics(
        HttpContext context,
        HttpRequestMessage downstreamRequest,
        HttpResponseMessage openAiResponse)
    {
        var hasRemainingRequests =
            openAiResponse.Headers.TryGetValues("x-ratelimit-remaining-requests", out var remainingRequests);
        var hasRemainingTokens =
            openAiResponse.Headers.TryGetValues("x-ratelimit-remaining-tokens", out var remainingTokens);

        var hostName = downstreamRequest.RequestUri!.Host.ToLowerInvariant();
        var modelName = context.Items.TryGetValue(HttpItemBagMappedModelName, out var stored)
            ? (string)stored!
            : string.Empty;

        if (hasRemainingRequests)
        {
            if (long.TryParse(remainingRequests?.FirstOrDefault(), out var val))
            {
                AICentralActivitySources.RecordGaugeMetric("remaining-requests", hostName, modelName, val);
            }
        }

        if (hasRemainingTokens)
        {
            if (long.TryParse(remainingTokens?.FirstOrDefault(), out var val))
            {
                AICentralActivitySources.RecordGaugeMetric("remaining-tokens", hostName, modelName, val);
            }
        }

        return Task.CompletedTask;
    }


    protected override Task CustomiseRequest(HttpContext context, AICallInformation aiCallInformation,
        HttpRequestMessage newRequest,
        string? newModelName)
    {
        if (aiCallInformation.IncomingCallDetails.RequestContent != null)
        {
            if (aiCallInformation.IncomingCallDetails.AICallType == AICallType.DALLE3 &&
                aiCallInformation.IncomingCallDetails.IncomingModelName != newModelName)
            {
                newRequest.Content = new StringContent(
                    AdjustDalle3ModelName(newModelName, aiCallInformation.IncomingCallDetails.RequestContent.DeepClone())
                        .ToString(Formatting.None),
                    Encoding.UTF8, "application/json");
            }
            else
            {
                newRequest.Content = new StringContent(
                    aiCallInformation.IncomingCallDetails.RequestContent.ToString(Formatting.None),
                    Encoding.UTF8, "application/json");
            }
        }
        else
        {
            newRequest.Content = new StreamContent(context.Request.Body);
        }

        _authHandler.ApplyAuthorisationToRequest(context.Request, newRequest);
        return Task.CompletedTask;
    }

    /// <summary>
    /// DALL-E 3 puts the model name in the URL as-well as the body. So to map models, we need to adjust the body.
    /// </summary>
    /// <param name="newModelName"></param>
    /// <param name="deepClone"></param>
    /// <returns></returns>
    private JToken AdjustDalle3ModelName(string? newModelName, JToken deepClone)
    {
        deepClone["model"] = newModelName;
        return deepClone;
    }

    /// <summary>
    /// Azure Open AI uses an async pattern for actions like image generation. We need to tweak the operation-location
    /// header else the request to look for the status won't work. 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="openAiResponse"></param>
    /// <returns></returns>
    protected override Dictionary<string, StringValues> CustomSanitiseHeaders(HttpContext context,
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
            _ => string.Empty
        };

        var incomingQuery = aiCallInformation.QueryString;
        incomingQuery.Remove(AICentralHeaders.AzureOpenAIHostAffinityHeader);

        return aiCallInformation.IncomingCallDetails.AICallType == AICallType.Other
            ? QueryHelpers.AddQueryString($"{_languageUrl}{context.Request.Path}", aiCallInformation.QueryString)
            : QueryHelpers.AddQueryString($"{_languageUrl}/openai/deployments/{mappedModelName}/{pathPiece}",
                aiCallInformation.QueryString);
    }
}