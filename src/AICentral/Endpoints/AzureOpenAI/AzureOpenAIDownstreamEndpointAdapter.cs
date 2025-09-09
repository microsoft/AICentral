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
    private readonly bool _logMissingModelMappingsAsInformation;

    public AzureOpenAIDownstreamEndpointAdapter(string id,
        string languageUrl,
        string endpointName,
        Dictionary<string, string> modelMappings,
        Dictionary<string, string> assistantMappings,
        IEndpointAuthorisationHandler authHandler, 
        bool enforceMappedModels, 
        bool autoPopulateEmptyUserId,
        bool logMissingModelMappingsAsInformation): base(id, new Uri(languageUrl), endpointName, modelMappings, assistantMappings, autoPopulateEmptyUserId)
    {
        _authHandler = authHandler;
        _enforceMappedModels = enforceMappedModels;
        _logMissingModelMappingsAsInformation = logMissingModelMappingsAsInformation;
    }

    /// <summary>
    /// Azure Open AI uses an async pattern for some actions like DALL-E 2 image generation. We need to tweak the operation-location
    /// header else the request to look for the status won't work. 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="openAiResponse"></param>
    /// <param name="proxiedHeaders"></param>
    /// <returns></returns>
    protected override void CustomSanitiseHeaders(IRequestContext context, HttpResponseMessage openAiResponse, Dictionary<string, StringValues> proxiedHeaders)
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

    protected override bool IsMissingModelMappingSerious()
    {
        return !_logMissingModelMappingsAsInformation;
    }

    private string AdjustAzureOpenAILocationToAICentralHost(
        IRequestContext context,
        KeyValuePair<string, IEnumerable<string>> header)
    {
        var locationRaw = header.Value.Single();
        var location = new Uri(locationRaw);
        var queryParts = QueryHelpers.ParseQuery(location.Query);
        queryParts.Add(QueryPartNames.AzureOpenAIHostAffinityQueryStringName, EndpointName);

        var builder = new UriBuilder(
            context.RequestScheme,
            context.RequestHost.Host,
            context.RequestHost.Port ?? 443,
            location.AbsolutePath
        );
        return QueryHelpers.AddQueryString(builder.ToString(), queryParts);
    }

    protected override Task ApplyAuthorisation(IRequestContext context, HttpRequestMessage newRequest)
    {
        return _authHandler.ApplyAuthorisationToRequest(context, newRequest);
    }
    
    
    protected override string BuildUri(
        IRequestContext context, 
        IncomingCallDetails aiCallInformation, 
        string? incomingAssistantName, 
        string? mappedAssistantName,
        string? incomingModelName,
        string? mappedModelName)
    {
        var pathPiece = aiCallInformation.AICallType switch
        {
            AICallType.Files => context.RequestPath, //affinity will ensure the request is going to the right place
            AICallType.Threads => context.RequestPath, //affinity will ensure the request is going to the right place
            AICallType.Assistants => incomingAssistantName != null ? context.RequestPath.Value!.Replace(incomingAssistantName, mappedAssistantName) : context.RequestPath,
            _ => incomingModelName != null && mappedModelName != null ? context.RequestPath.Value!.Replace($"/{incomingModelName}/", $"/{mappedModelName}/") : context.RequestPath
        };

        var newRequestString = new Uri(BaseUrl, pathPiece).AbsoluteUri;
        return QueryHelpers.AddQueryString(newRequestString, context.QueryString);
    }
    
}