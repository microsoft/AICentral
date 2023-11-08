using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

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

    protected override HttpRequestMessage BuildRequest(
        HttpContext context,
        AICallInformation aiCallInformation,
        string mappedModelName)
    {
        aiCallInformation.QueryString.TryAdd("api-version", "2023-05-15");

        var pathPiece = aiCallInformation.AICallType switch
        {
            AICallType.Chat => "chat/completions",
            AICallType.Completions => "completions",
            AICallType.Embeddings => "embeddings",
            _ => string.Empty
        };

        var requestUri = aiCallInformation.AICallType == AICallType.Other
            ? aiCallInformation.AIServiceType == AIServiceType.AzureOpenAI
                ? $"{_languageUrl}{context.Request.Path}"
                : throw new NotSupportedException("Unable to dispatch 'other' Open AI request to Azure Open AI")
            : $"{_languageUrl}/openai/deployments/{mappedModelName}/{pathPiece}";

        var newRequest = context.Request.Method.Equals("post", StringComparison.InvariantCultureIgnoreCase)
            ? new HttpRequestMessage(
                HttpMethod.Post,
                QueryHelpers.AddQueryString(requestUri, aiCallInformation.QueryString)
            )
            {
                Content = new StringContent(aiCallInformation.RequestContent!.ToString(Formatting.None), Encoding.UTF8,
                    "application/json")
            }
            : new HttpRequestMessage(
                HttpMethod.Get,
                QueryHelpers.AddQueryString(requestUri, aiCallInformation.QueryString));

        _authHandler.ApplyAuthorisationToRequest(context.Request, newRequest);
        return newRequest;
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
}