using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral.Steps.Endpoints.OpenAILike.OpenAI;

public class OpenAIEndpointDispatcher : OpenAILikeEndpointDispatcher
{
    const string OpenAIV1 = "https://api.openai.com/v1";
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

    protected override HttpRequestMessage BuildRequest(
        HttpContext context,
        AICallInformation aiCallInformation,
        string mappedModelName)
    {
        var pathPiece = aiCallInformation.AICallType switch
        {
            AICallType.Chat => "chat/completions",
            AICallType.Completions => "completions",
            AICallType.Embeddings => "embeddings",
            _ => string.Empty
        };

        var requestUri = aiCallInformation.AICallType == AICallType.Other
            ? aiCallInformation.AIServiceType == AIServiceType.OpenAI
                ? $"{OpenAIV1}{context.Request.Path}"
                : throw new InvalidOperationException("Unable to dispatch 'other' Azure Open AI request to Open AI")
            : $"{OpenAIV1}/{pathPiece}";

        var request = context.Request.Method.Equals("post", StringComparison.InvariantCultureIgnoreCase)
            ? new HttpRequestMessage(
                HttpMethod.Post,
                requestUri
            )
            {
                Content = GetContentWithModelNameIfApplicable(aiCallInformation, mappedModelName)
            }
            : new HttpRequestMessage(
                HttpMethod.Get,
                requestUri);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        if (!string.IsNullOrWhiteSpace(_organization))
        {
            request.Headers.Add("OpenAI-Organization", _organization);
        }

        return request;
    }

    private static StringContent GetContentWithModelNameIfApplicable(AICallInformation aiCallInformation,
        string? mappedModelName)
    {
        var content =
            aiCallInformation.AIServiceType == AIServiceType.AzureOpenAI &&
            aiCallInformation.AICallType != AICallType.Other
                ? AddModelName(aiCallInformation.RequestContent!.DeepClone(), mappedModelName!)
                : aiCallInformation.RequestContent!.DeepClone();
        return new StringContent(aiCallInformation.RequestContent!.ToString(Formatting.None), Encoding.UTF8,
            "application/json");
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

    public override Dictionary<string, StringValues> SanitiseHeaders(HttpContext context, HttpResponseMessage openAiResponse)
    {
        return openAiResponse.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray()));
    }

    protected override string HostUriBase => OpenAIV1;
}