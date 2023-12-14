using System.Net.Http.Headers;
using System.Text;
using AICentral.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace AICentral.OpenAI.OpenAI;

public class OpenAIEndpointRequestResponseHandler : OpenAILikeEndpointRequestResponseHandler
{
    internal const string OpenAIV1 = "https://api.openai.com";
    private readonly string? _organization;
    private readonly string _apiKey;

    public OpenAIEndpointRequestResponseHandler(string id,
        string endpointName,
        Dictionary<string, string> modelMappings,
        string apiKey,
        string? organization) : base(id, OpenAIV1, endpointName, modelMappings)
    {
        _organization = organization;
        _apiKey = apiKey;
    }

    protected override Task ExtractDiagnostics(
        HttpContext context,
        HttpRequestMessage downstreamRequest, 
        HttpResponseMessage openAiResponse)
    {
        return Task.CompletedTask;
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
            newRequest.Content = HandleMappedModelName(aiCallInformation, mappedModelName);
        }
        else
        {
            newRequest.Content = new StreamContent(context.Request.Body);
            newRequest.Content.Headers.Add("Content-Type", context.Request.Headers.ContentType.ToString());
        }

        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        if (!string.IsNullOrWhiteSpace(_organization))
        {
            newRequest.Headers.Add("OpenAI-Organization", _organization);
        }

        return Task.CompletedTask;
    }

    private static StringContent HandleMappedModelName(AICallInformation aiCallInformation,
        string? mappedModelName)
    {
        if (aiCallInformation.IncomingCallDetails.AICallType == AICallType.DALLE3)
        {
            //no model name for this
            var content = aiCallInformation.IncomingCallDetails.RequestContent!.DeepClone();
            content["model"]!.Parent!.Remove();
            return new StringContent(content.ToString(), Encoding.UTF8, "application/json");
        }
        return aiCallInformation.IncomingCallDetails.IncomingModelName == mappedModelName
            ? new StringContent(aiCallInformation.IncomingCallDetails.RequestContent!.ToString(), Encoding.UTF8,
                "application/json")
            : new StringContent(
                AddModelName(aiCallInformation.IncomingCallDetails.RequestContent!.DeepClone(), mappedModelName!)
                    .ToString(),
                Encoding.UTF8, "application/json");
    }

    protected override string BuildUri(HttpContext context, AICallInformation aiCallInformation, string? _)
    {
        var pathPiece = aiCallInformation.IncomingCallDetails.AICallType switch
        {
            AICallType.Chat => "chat/completions",
            AICallType.Completions => "completions",
            AICallType.Embeddings => "embeddings",
            AICallType.DALLE3 => "images/generations",
            _ => string.Empty
        };

        var requestUri = string.IsNullOrWhiteSpace(pathPiece)
            ? throw new InvalidOperationException(
                "Unable to forward this request from an Azure Open AI request to Open AI")
            : $"{OpenAIV1}/v1/{pathPiece}";

        return requestUri;
    }

    private static JToken AddModelName(JToken deepClone, string mappedModelName)
    {
        deepClone["model"] = mappedModelName;
        return deepClone;
    }

    protected override Dictionary<string, StringValues> CustomSanitiseHeaders(HttpContext context,
        HttpResponseMessage openAiResponse)
    {
        return openAiResponse.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value.ToArray()));
    }
}