using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral.PipelineComponents.Endpoints.OpenAILike.AzureOpenAI;

public class AzureOpenAIEndpointDispatcher : OpenAILikeEndpointDispatcher
{
    private readonly string _languageUrl;
    private readonly IEndpointAuthorisationHandler _authHandler;

    public AzureOpenAIEndpointDispatcher(
        string id,
        string languageUrl,
        Dictionary<string, string> modelMappings,
        IEndpointAuthorisationHandler authHandler): base(id, modelMappings)
    {
        _languageUrl = languageUrl;
        _authHandler = authHandler;
    }
    
    protected override HttpRequestMessage BuildRequest(
        HttpContext context, 
        AICallInformation aiCallInformation,
        string mappedModelName)
    {
        
        var newRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_languageUrl}/openai/deployments/{mappedModelName}/{aiCallInformation.RemainingUrl}"
        )
        {
            Content = new StringContent(aiCallInformation.RequestContent.ToString(Formatting.None), Encoding.UTF8, "application/json")
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