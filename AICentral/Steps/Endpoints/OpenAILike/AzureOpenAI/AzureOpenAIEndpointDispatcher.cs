using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.Endpoints.OpenAILike.AzureOpenAI;

public class AzureOpenAIEndpointDispatcher : OpenAILikeEndpointDispatcher
{
    private static readonly string[] HeaderPrefixesToCopy = { "x-", "apim", "operation-location" };

    private readonly string _languageUrl;
    private readonly IEndpointAuthorisationHandler _authHandler;

    public AzureOpenAIEndpointDispatcher(
        string id,
        string languageUrl,
        Dictionary<string, string> modelMappings,
        IEndpointAuthorisationHandler authHandler) : base(id, modelMappings)
    {
        _languageUrl = languageUrl.EndsWith('/') ? languageUrl[..^1] : languageUrl;
        _authHandler = authHandler;
    }
    
    protected override Task CustomiseRequest(HttpContext context, AICallInformation aiCallInformation,
        HttpRequestMessage newRequest,
        string? newModelName)
    {
        
        //if there is a model change then set the model on a new outbound JSON request. Else copy the content with no changes
        newRequest.Content =  new StreamContent(context.Request.Body);
        _authHandler.ApplyAuthorisationToRequest(context.Request, newRequest);
        return Task.CompletedTask;
    }

    public override object WriteDebug()
    {
        return new
        {
            Type = "AzureOpenAI",
            Url = _languageUrl,
            Common = base.WriteDebug(),
            Auth = _authHandler.WriteDebug()
        };
    }

    public override Dictionary<string, StringValues> SanitiseHeaders(HttpContext context,
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

    private string AdjustAzureOpenAILocationToAICentralHost(HttpContext context,
        KeyValuePair<string, IEnumerable<string>> header)
    {
        var locationRaw = header.Value.Single();
        var location = new Uri(locationRaw);
        var builder = new UriBuilder(
            context.Request.Scheme,
            context.Request.Host.Host,
            context.Request.Host.Port ?? 443,
            location.AbsolutePath,
            location.Query
        );

        return builder.Uri.AbsoluteUri;
    }

    protected override string HostUriBase => _languageUrl;

    protected override string BuildUri(HttpContext context, AICallInformation aiCallInformation,
        string? mappedModelName)
    {
        aiCallInformation.QueryString.TryAdd("api-version", "2023-05-15");

        var pathPiece = aiCallInformation.IncomingCallDetails.AICallType switch
        {
            AICallType.Chat => "chat/completions",
            AICallType.Completions => "completions",
            AICallType.Embeddings => "embeddings",
            _ => string.Empty
        };

        return aiCallInformation.IncomingCallDetails.AICallType == AICallType.Other
            ? aiCallInformation.IncomingCallDetails.ServiceType == AIServiceType.AzureOpenAI
                ? QueryHelpers.AddQueryString($"{_languageUrl}{context.Request.Path}", aiCallInformation.QueryString)
                : throw new InvalidOperationException("Unable to forward this request from an Open AI request to Azure Open AI")
            : QueryHelpers.AddQueryString($"{_languageUrl}/openai/deployments/{mappedModelName}/{pathPiece}",
                aiCallInformation.QueryString);
    }
}