using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace AICentral.Steps.Endpoints.OpenAILike.OpenAI;

public class OpenAIEndpointDispatcher : OpenAILikeEndpointDispatcher
{
    const string OpenAIV1 = "https://api.openai.com";
    private readonly string? _organization;
    private readonly string _apiKey;

    public OpenAIEndpointDispatcher(string id,
        Dictionary<string, string> modelMappings,
        string apiKey,
        string? organization) : base(id, modelMappings)
    {
        _organization = organization;
        _apiKey = apiKey;
    }

    protected override Task CustomiseRequest(
        HttpContext context,
        AICallInformation aiCallInformation,
        HttpRequestMessage newRequest,
        string? mappedModelName)
    {

        //if there is a model change then set the model on a new outbound JSON request. Else copy the content with no changes
        if (aiCallInformation.IncomingCallDetails.AICallType != AICallType.Other)
        {
            newRequest.Content = IncomingContentWithInjectedModelName(aiCallInformation, mappedModelName);
        }
        else
        {
            newRequest.Content =  new StreamContent(context.Request.Body);
        }

        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        if (!string.IsNullOrWhiteSpace(_organization))
        {
            newRequest.Headers.Add("OpenAI-Organization", _organization);
        }
        
        return Task.CompletedTask;
    }

    private static StringContent IncomingContentWithInjectedModelName(AICallInformation aiCallInformation, string? mappedModelName)
    {
        return aiCallInformation.IncomingCallDetails.IncomingModelName == mappedModelName
            ? new StringContent(aiCallInformation.RequestContent!.ToString(), Encoding.UTF8,
                "application/json")
            : new StringContent(
                AddModelName(aiCallInformation.RequestContent!.DeepClone(), mappedModelName!).ToString(),
                Encoding.UTF8, "application/json");
    }

    protected override string BuildUri(HttpContext context, AICallInformation aiCallInformation, string? _)
    {
        var pathPiece = aiCallInformation.IncomingCallDetails.AICallType switch
        {
            AICallType.Chat => "chat/completions",
            AICallType.Completions => "completions",
            AICallType.Embeddings => "embeddings",
            _ => string.Empty
        };

        var requestUri = aiCallInformation.IncomingCallDetails.AICallType == AICallType.Other
            ? aiCallInformation.IncomingCallDetails.ServiceType == AIServiceType.OpenAI
                ? $"{OpenAIV1}{context.Request.Path}"
                : throw new InvalidOperationException("Unable to dispatch 'other' Azure Open AI request to Open AI")
            : $"{OpenAIV1}/v1/{pathPiece}";
        
        return requestUri;
    }

    private static JToken AddModelName(JToken deepClone, string mappedModelName)
    {
        deepClone["model"] = mappedModelName;
        return deepClone;
    }

    public override object WriteDebug()
    {
        return new
        {
            Type = "OpenAI",
            Url = OpenAIV1,
            Common = base.WriteDebug()
        };
    }

    public override Dictionary<string, StringValues> SanitiseHeaders(HttpContext context,
        HttpResponseMessage openAiResponse)
    {
        return openAiResponse.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray()));
    }

    protected override string HostUriBase => OpenAIV1;
}