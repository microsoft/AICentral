using AICentral.Core;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace AICentral.Endpoints.AzureOpenAI;

public class AzureOpenAIDownstreamEndpointAdapter : OpenAILikeDownstreamEndpointAdapter
{
    protected override string[] HeadersToIgnore => ["x-aicentral-affinity-key", "host", "authorization", "api-key"];
    protected override string[] HeaderPrefixesToCopy => ["x-", "apim", "operation-location", "ms-azureml"];
    private readonly IEndpointAuthorisationHandler _authHandler;
    private readonly bool _enforceMappedModels;

    public AzureOpenAIDownstreamEndpointAdapter(string id,
        string languageUrl,
        string endpointName,
        Dictionary<string, string> modelMappings,
        Dictionary<string, string> assistantMappings,
        IEndpointAuthorisationHandler authHandler, 
        bool enforceMappedModels): base(id, new Uri(languageUrl), endpointName, modelMappings, assistantMappings)
    {
        _authHandler = authHandler;
        _enforceMappedModels = enforceMappedModels;
    }

    /// <summary>
    /// Azure Open AI uses an async pattern for some actions like DALL-E 2 image generation. We need to tweak the operation-location
    /// header else the request to look for the status won't work. 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="openAiResponse"></param>
    /// <param name="proxiedHeaders"></param>
    /// <returns></returns>
    protected override void CustomSanitiseHeaders(HttpContext context, HttpResponseMessage openAiResponse, Dictionary<string, StringValues> proxiedHeaders)
    {
        foreach (var header in openAiResponse.Headers)
        {
            if (HeaderPrefixesToCopy.Any(x => header.Key.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                if (header.Key.Equals("operation-location", StringComparison.InvariantCultureIgnoreCase))
                {
                    proxiedHeaders[header.Key] = AdjustAzureOpenAILocationToAICentralHost(context, header);
                }
            }
        }
    }

    protected override bool IsFixedModelName(AICallType callType, string? callInformationIncomingModelName, out string? fixedModelName)
    {
        if (_enforceMappedModels)
        {
            fixedModelName = null;
            return false;
        }

        fixedModelName = callInformationIncomingModelName;
        return true;
    }

    private string AdjustAzureOpenAILocationToAICentralHost(
        HttpContext context,
        KeyValuePair<string, IEnumerable<string>> header)
    {
        var locationRaw = header.Value.Single();
        var location = new Uri(locationRaw);
        var queryParts = QueryHelpers.ParseQuery(location.Query);
        queryParts.Add(QueryPartNames.AzureOpenAIHostAffinityQueryStringName, EndpointName);

        var builder = new UriBuilder(
            context.Request.Scheme,
            context.Request.Host.Host,
            context.Request.Host.Port ?? 443,
            location.AbsolutePath
        );
        return QueryHelpers.AddQueryString(builder.ToString(), queryParts);
    }

    protected override Task ApplyAuthorisation(HttpContext context, HttpRequestMessage newRequest)
    {
        return _authHandler.ApplyAuthorisationToRequest(context.Request, newRequest);
    }
    
    
    protected override string BuildUri(
        HttpContext context, 
        IncomingCallDetails aiCallInformation, 
        string? incomingAssistantName, 
        string? mappedAssistantName,
        string? incomingModelName,
        string? mappedModelName)
    {
        var pathPiece = aiCallInformation.AICallType switch
        {
            AICallType.Files => context.Request.Path.Value!, //affinity will ensure the request is going to the right place
            AICallType.Threads => context.Request.Path.Value!, //affinity will ensure the request is going to the right place
            AICallType.Assistants => context.Request.Path.Value!.Replace(incomingAssistantName ?? string.Empty, mappedAssistantName),
            _ => incomingModelName != null && mappedModelName != null ? context.Request.Path.Value!.Replace($"/{incomingModelName}/", $"/{mappedModelName}/") : context.Request.Path
        };

        var newRequestString = new Uri(BaseUrl, pathPiece).AbsoluteUri;
        return QueryHelpers.AddQueryString(newRequestString, aiCallInformation.QueryString ?? new Dictionary<string, StringValues>());
    }
    
}