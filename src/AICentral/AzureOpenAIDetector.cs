using System.Text.Json;
using AICentral.Core;
using Microsoft.AspNetCore.WebUtilities;

namespace AICentral;

public class AzureOpenAIDetector
{
    public async Task<IncomingCallDetails> Detect(string pipelineName, string deploymentName, AICallType callType, HttpRequest request, CancellationToken cancellationToken)
    {
        return callType switch
        {
            AICallType.Chat => await DetectChat(pipelineName, deploymentName, request, cancellationToken),
            AICallType.Completions => await DetectCompletions(pipelineName, deploymentName, request, cancellationToken),
            AICallType.Embeddings => await DetectEmbeddings(pipelineName, deploymentName, request, cancellationToken),
            AICallType.Transcription => await DetectTranscription(pipelineName, deploymentName, request, cancellationToken),
            AICallType.Translation => await DetectTranslation(pipelineName, deploymentName, request, cancellationToken),
            AICallType.DALLE2 => await DetectDalle2(pipelineName, request, cancellationToken),
            AICallType.DALLE3 => await DetectDalle3(pipelineName, deploymentName, request, cancellationToken),
            _ => new IncomingCallDetails(pipelineName, callType, null, null, null, QueryHelpers.ParseQuery(request.QueryString.Value))
        };
    }

    private async Task<IncomingCallDetails> DetectChat(string pipelineName, string deploymentName, HttpRequest request, CancellationToken cancellationToken)
    {
        var requestContent = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Chat,
            string.Join(
                '\n',
                requestContent.RootElement.GetProperty("messages").EnumerateArray()
                    .Select(x => x.GetProperty("content").GetString())),
            deploymentName,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value));
    }

    private async Task<IncomingCallDetails> DetectCompletions(string pipelineName, string deploymentName, HttpRequest request, CancellationToken cancellationToken)
    {
        var requestContent = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Completions,
            string.Join('\n', requestContent.RootElement.GetProperty("prompt").EnumerateArray().Select(x => x.GetString())),
            deploymentName,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value));
    }

    private async Task<IncomingCallDetails> DetectEmbeddings(string pipelineName, string deploymentName, HttpRequest request, CancellationToken cancellationToken)
    {
        var requestContent = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Embeddings,
            requestContent.RootElement.GetProperty("input").GetString() ?? string.Empty,
            deploymentName,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value));
    }

    private async Task<IncomingCallDetails> DetectTranscription(string pipelineName, string deploymentName, HttpRequest request, CancellationToken cancellationToken)
    {
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Transcription,
            null,
            deploymentName,
            null,
            QueryHelpers.ParseQuery(request.QueryString.Value));
    }

    private async Task<IncomingCallDetails> DetectTranslation(string pipelineName, string deploymentName, HttpRequest request, CancellationToken cancellationToken)
    {
        return new IncomingCallDetails(
            pipelineName,
            AICallType.Translation,
            null,
            deploymentName,
            null,
            QueryHelpers.ParseQuery(request.QueryString.Value));
    }
    
    private async Task<IncomingCallDetails> DetectDalle2(string pipelineName, HttpRequest request, CancellationToken cancellationToken)
    {
        var requestContent = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
        return new IncomingCallDetails(
            pipelineName,
            AICallType.DALLE2,
            null,
            null,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value));
    }
    
    private async Task<IncomingCallDetails> DetectDalle3(string pipelineName, string deploymentName, HttpRequest request, CancellationToken cancellationToken)
    {
        var requestContent = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
        return new IncomingCallDetails(
            pipelineName,
            AICallType.DALLE3,
            null,
            deploymentName,
            requestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value));
    }
}