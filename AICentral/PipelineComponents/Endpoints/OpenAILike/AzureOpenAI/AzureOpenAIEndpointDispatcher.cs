using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace AICentral.PipelineComponents.Endpoints.OpenAILike.AzureOpenAI;

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
        _languageUrl = languageUrl;
        _authHandler = authHandler;
    }

    protected override HttpRequestMessage BuildRequest(
        HttpContext context,
        AICallInformation aiCallInformation,
        string mappedModelName)
    {
        aiCallInformation.QueryString.TryAdd("api-version", "2023-05-15");
        var requestUri =
            QueryHelpers.AddQueryString(
                $"{_languageUrl}/openai/deployments/{mappedModelName}/{aiCallInformation.RemainingUrl}",
                aiCallInformation.QueryString);

        var newRequest = new HttpRequestMessage(
            HttpMethod.Post,
            requestUri
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