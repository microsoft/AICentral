using AICentral.Core;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace AICentral.Endpoints.AzureOpenAI;

public class AzureOpenAIDownstreamEndpointAdapter : IDownstreamEndpointAdapter
{
    private static readonly string[] HeadersToIgnore = { "host", "authorization", "api-key" };
    private static readonly string[] HeaderPrefixesToCopy = { "x-", "apim", "operation-location" };
    private readonly IEndpointAuthorisationHandler _authHandler;

    public AzureOpenAIDownstreamEndpointAdapter(
        string id,
        string languageUrl,
        string endpointName,
        IEndpointAuthorisationHandler authHandler)
    {
        Id = id;
        EndpointName = endpointName;
        BaseUrl = languageUrl.EndsWith('/') ? languageUrl[..^1] : languageUrl;
        _authHandler = authHandler;
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
            openAiResponse.Headers.Contains("operation-location"),
            didHaveTokenLimitHeader ? remainingTokens : null,
            didHaveRequestLimitHeader ? remainingRequests : null));
    }


    /// <summary>
    /// Azure Open AI uses an async pattern for some actions like DALL-E 2 image generation. We need to tweak the operation-location
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

    public async Task<Either<AIRequest, IResult>> BuildRequest(IncomingCallDetails incomingCall, HttpContext context)
    {
        var newRequest = new HttpRequestMessage(
            new HttpMethod(context.Request.Method),
            $"{BaseUrl}{context.Request.GetEncodedPathAndQuery()}");

        if (incomingCall.RequestContent != null)
        {
            newRequest.Content = new StringContent(incomingCall.RequestContent!.ToString());
        }
        else
        {
            context.Request.Body.Position = 0;
            newRequest.Content = new StreamContent(context.Request.Body);
            newRequest.Content.Headers.Add("Content-Type", context.Request.Headers.ContentType.ToString());
        }

        await _authHandler.ApplyAuthorisationToRequest(context.Request, newRequest);

        foreach (var header in context.Request.Headers)
        {
            if (HeadersToIgnore.Contains(header.Key.ToLowerInvariant())) continue;
            newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        return new Either<AIRequest, IResult>(
            new AIRequest(newRequest, incomingCall.IncomingModelName));
    }

    public string Id { get; }
    public string BaseUrl { get; }
    public string EndpointName { get; }
}