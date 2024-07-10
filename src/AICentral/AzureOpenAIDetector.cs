using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AICentral.Core;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace AICentral;

public class AzureOpenAIDetector
{
    public async Task<IncomingCallDetails> Detect(string pipelineName, string? deploymentName, string? assistantName, AICallType callType, HttpRequest request, CancellationToken cancellationToken)
    {
        return callType switch
        {
            AICallType.Chat => await DetectChat(pipelineName, deploymentName!, request, cancellationToken),
            AICallType.Completions => await DetectCompletions(pipelineName, deploymentName!, request, cancellationToken),
            AICallType.Embeddings => await DetectEmbeddings(pipelineName, deploymentName!, request, cancellationToken),
            AICallType.Transcription => DetectTranscription(pipelineName, deploymentName!, request),
            AICallType.Translation => DetectTranslation(pipelineName, deploymentName!, request),
            AICallType.Operations => DetectOperations(pipelineName, request),
            AICallType.DALLE2 => await DetectDalle2(pipelineName, request, cancellationToken),
            AICallType.DALLE3 => await DetectDalle3(pipelineName, deploymentName!, request, cancellationToken),
            AICallType.Assistants => await DetectAssistant(pipelineName, assistantName, request, cancellationToken),
            AICallType.Threads => await DetectThread(pipelineName, request, cancellationToken),
            AICallType.Files => DetectFile(pipelineName, request),
            _ => new IncomingCallDetails(pipelineName, callType, AICallResponseType.NonStreaming, null, null, null, null, QueryHelpers.ParseQuery(request.QueryString.Value), null)
        };
    }

    private async Task<IncomingCallDetails> DetectChat(string pipelineName, string deploymentName, HttpRequest request, CancellationToken cancellationToken)
    {
        var requestContent = (await JsonNode.ParseAsync(request.Body, cancellationToken: cancellationToken))!;
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Chat,
            requestContent.TryGetProperty("stream", out var stream) ? 
                stream.GetValue<bool>()
                    ? AICallResponseType.Streaming 
                    : AICallResponseType.NonStreaming 
                : AICallResponseType.NonStreaming,
            string.Join(
                '\n',
                requestContent["messages"]!.AsArray()
                    .Select(x => GetTextContent(x!["content"]!))),
            deploymentName,
            null,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value),
            null);
    }

    private string GetTextContent(JsonNode contentProperty)
    {
        if (contentProperty.GetValueKind() == JsonValueKind.Array)
        {
            var arrayContent = new StringBuilder();
            foreach(var item in contentProperty.AsArray())
            {
                if (item!.TryGetProperty("type", out var typeElement))
                {
                    if (typeElement.GetValue<string>() == "text")
                    {
                        arrayContent.Append(item!["text"]!.GetValue<string>());
                        arrayContent.Append(' ');
                    }
                }
            }

            return arrayContent.ToString();
        }

        return contentProperty.GetValue<string>();
    }
    
    private async Task<IncomingCallDetails> DetectCompletions(string pipelineName, string deploymentName, HttpRequest request, CancellationToken cancellationToken)
    {
        var requestContent = (await JsonNode.ParseAsync(request.Body, cancellationToken: cancellationToken))!;
        var prompt = requestContent["prompt"]!;
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Completions,
            requestContent.TryGetProperty("stream", out var stream)
                ? stream.GetValue<bool>()
                    ? AICallResponseType.Streaming
                    : AICallResponseType.NonStreaming
                : AICallResponseType.NonStreaming,
            prompt.GetValueKind() == JsonValueKind.Array
                ? string.Join('\n',
                    requestContent["prompt"]!.AsArray().Select(x => x!.GetValue<string>()))
                : prompt.GetValue<string>(),
            deploymentName,
            null,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value),
            null);
    }

    private async Task<IncomingCallDetails> DetectEmbeddings(string pipelineName, string deploymentName, HttpRequest request, CancellationToken cancellationToken)
    {
        var requestContent = (await JsonNode.ParseAsync(request.Body, cancellationToken: cancellationToken))!;
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Embeddings,
            AICallResponseType.NonStreaming,
            GetEmbeddingContent(requestContent["input"]!),
            deploymentName,
            null,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value),
            null);
    }

    private string GetEmbeddingContent(JsonNode contentProperty)
    {
        if (contentProperty.GetValueKind() == JsonValueKind.Array)
        {
            var arrayContent = new StringBuilder();
            foreach(var item in contentProperty.AsArray())
            {
                arrayContent.Append(item!.GetValue<string>());
                arrayContent.Append(" ");
            }

            return arrayContent.ToString();
        }

        return contentProperty.GetValue<string>();
    }

    private IncomingCallDetails DetectTranscription(string pipelineName, string deploymentName, HttpRequest request)
    {
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Transcription,
            AICallResponseType.NonStreaming,
            null,
            deploymentName,
            null,
            null,
            QueryHelpers.ParseQuery(request.QueryString.Value),
            null);
    }

    private IncomingCallDetails DetectFile(string pipelineName, HttpRequest request)
    {
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Files,
            AICallResponseType.NonStreaming,
            null,
            null,
            null,
            null,
            QueryHelpers.ParseQuery(request.QueryString.Value),
            null);
    }

    private IncomingCallDetails DetectTranslation(string pipelineName, string deploymentName, HttpRequest request)
    {
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Translation,
            AICallResponseType.NonStreaming,
            null,
            deploymentName,
            null,
            null,
            QueryHelpers.ParseQuery(request.QueryString.Value),
            null);
    }
    
    private async Task<IncomingCallDetails> DetectDalle2(string pipelineName, HttpRequest request, CancellationToken cancellationToken)
    {
        var requestContent = (await JsonNode.ParseAsync(request.Body, cancellationToken: cancellationToken))!;
        return new IncomingCallDetails(
            pipelineName,
            AICallType.DALLE2,
            AICallResponseType.NonStreaming,
            null,
            null,
            null,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value),
            null);
    }
    
    private IncomingCallDetails DetectOperations(string pipelineName, HttpRequest request)
    {
        var queryString = QueryHelpers.ParseQuery(request.QueryString.Value);
        var endpointAffinity = LookForAffinityOnRequest(queryString);

        return new IncomingCallDetails(
            pipelineName,
            AICallType.Operations,
            AICallResponseType.NonStreaming,
            null,
            null,
            null,
            null,
            queryString,
            endpointAffinity);
    }
    
    private async Task<IncomingCallDetails> DetectDalle3(string pipelineName, string deploymentName, HttpRequest request, CancellationToken cancellationToken)
    {
        var requestContent = (await JsonNode.ParseAsync(request.Body, cancellationToken: cancellationToken))!;
        return new IncomingCallDetails(
            pipelineName,
            AICallType.DALLE3,
            AICallResponseType.NonStreaming,
            null,
            deploymentName,
            null,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value),
            null);
    }
    
    private async Task<IncomingCallDetails> DetectAssistant(string pipelineName, string? assistantName, HttpRequest request, CancellationToken cancellationToken)
    {
        JsonNode? requestContent = null;
        if (request.HasJsonContentType() && (request.Method.Equals("post", StringComparison.InvariantCultureIgnoreCase)  || request.Method.Equals("put", StringComparison.InvariantCultureIgnoreCase)))
        {
            requestContent = (await JsonNode.ParseAsync(request.Body, cancellationToken: cancellationToken))!;
        }

        return new IncomingCallDetails(
            pipelineName,
            AICallType.Assistants,
            AICallResponseType.NonStreaming,
            null,
            null,
            assistantName,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value),
            null);
    }
    
    private async Task<IncomingCallDetails> DetectThread(string pipelineName, HttpRequest request, CancellationToken cancellationToken)
    {
        JsonNode? requestContent = null;
        string? assistantId = null;
        if (request.HasJsonContentType() && (request.Method.Equals("post", StringComparison.InvariantCultureIgnoreCase)  || request.Method.Equals("put", StringComparison.InvariantCultureIgnoreCase)))
        {
            requestContent = (await JsonNode.ParseAsync(request.Body, cancellationToken: cancellationToken))!;
            assistantId = requestContent.TryGetProperty("assistant_id", out var elem)
                ? elem.GetValue<string>()
                : null;
        }
        
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Threads,
            AICallResponseType.NonStreaming,
            null,
            null,
            assistantId,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value),
            null);
    }
    
    /// <summary>
    /// A consumer may want affinity to a particular endpoint.
    /// </summary>
    /// <remarks>
    /// DALLE-2 on Azure Open AI is a good example where the operation is asynchronous involving multiple calls. 
    /// </remarks>
    /// <param name="requestQueryString"></param>
    /// <returns></returns>
    private string? LookForAffinityOnRequest(Dictionary<string, StringValues> requestQueryString)
    {
        if (requestQueryString.TryGetValue(QueryPartNames.AzureOpenAIHostAffinityQueryStringName,
                out var affinityMarker))
        {
            if (affinityMarker.Count == 1)
            {
                requestQueryString.Remove(QueryPartNames.AzureOpenAIHostAffinityQueryStringName);
                return affinityMarker.Single();
            }
        }

        return null;
    }
}