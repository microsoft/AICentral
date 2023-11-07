using System.Net;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace AICentral.Steps.Endpoints.OpenAILike.AzureOpenAI;

public class AzureOpenAIEndpointDispatcher : OpenAILikeEndpointDispatcher
{
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
            
        var newRequest = new HttpRequestMessage(
            HttpMethod.Post,
            QueryHelpers.AddQueryString(requestUri, aiCallInformation.QueryString)
        )
        {
            Content = new StringContent(aiCallInformation.RequestContent.ToString(Formatting.None), Encoding.UTF8,
                "application/json")
        };
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

    protected override string HostUriBase => _languageUrl;
}