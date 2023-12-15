using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using AICentral.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;

namespace AICentral.OpenAI;

public abstract class OpenAILikeEndpointRequestResponseHandler : IEndpointRequestResponseHandler
{
    private readonly Dictionary<string, string> _modelMappings;
    private static readonly HashSet<string> HeadersToIgnore = new(new[] { "host", "authorization", "api-key" });

    protected OpenAILikeEndpointRequestResponseHandler(
        string id,
        string baseUrl,
        string endpointName,
        Dictionary<string, string> modelMappings)
    {
        EndpointName = endpointName;
        Id = id;
        BaseUrl = baseUrl;
        _modelMappings = modelMappings;
    }

    /// <summary>
    /// Opportunity to pull specific diagnostics and, for example, raise your own telemetry events.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="downstreamRequest"></param>
    /// <param name="openAiResponse"></param>
    /// <returns></returns>
    protected abstract Task<ResponseMetadata> PreProcess(
        HttpContext context,
        AIRequest downstreamRequest,
        HttpResponseMessage openAiResponse);

    private static bool MappedModelFoundAsEmptyString(AICallInformation callInformation, string mappedModelName)
    {
        return callInformation.IncomingCallDetails.AICallType != AICallType.Other && mappedModelName == string.Empty;
    }

    private async Task<HttpRequestMessage> BuildNewRequest(HttpContext context, AICallInformation callInformation,
        string? mappedModelName)
    {
        var newRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method),
            BuildUri(context, callInformation, mappedModelName));

        foreach (var header in context.Request.Headers)
        {
            if (HeadersToIgnore.Contains(header.Key.ToLowerInvariant())) continue;

            if (!newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) &&
                newRequest.Content != null)
            {
                newRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        await CustomiseRequest(context, callInformation, newRequest, mappedModelName);

        return newRequest;
    }

    protected abstract Task CustomiseRequest(HttpContext context, AICallInformation callInformation,
        HttpRequestMessage newRequest, string? newModelName);

    protected abstract string BuildUri(HttpContext context, AICallInformation aiCallInformation,
        string? mappedModelName);

    public string Id { get; }
    public string BaseUrl { get; }
    public string EndpointName { get; }

    public async Task<Either<AIRequest, IResult>> BuildRequest(AICallInformation callInformation, HttpContext context)
    {
        var incomingModelName = callInformation.IncomingCallDetails.IncomingModelName ?? string.Empty;

        var mappedModelName = _modelMappings.GetValueOrDefault(incomingModelName, incomingModelName);

        if (MappedModelFoundAsEmptyString(callInformation, mappedModelName))
        {
            return new Either<AIRequest, IResult>(Results.NotFound(new { message = "Unknown model" }));
        }

        try
        {
            return new Either<AIRequest, IResult>(
                new AIRequest(await BuildNewRequest(context, callInformation, mappedModelName), mappedModelName));
        }
        catch (InvalidOperationException ie)
        {
            return new Either<AIRequest, IResult>(Results.BadRequest(new { message = ie.Message }));
        }
    }

    public Task<ResponseMetadata> ExtractResponseMetadata(
        IncomingCallDetails callInformationIncomingCallDetails,
        HttpContext context,
        AIRequest newRequest,
        HttpResponseMessage openAiResponse)
    {
        return PreProcess(context, newRequest, openAiResponse);
    }


    protected static async Task<HttpContent> CreateDownstreamResponseWithMappedModelName(
        AICallInformation aiCallInformation,
        HttpRequest incomingRequest,
        string? mappedModelName)
    {
        if (aiCallInformation.IncomingCallDetails.AICallType == AICallType.DALLE3)
        {
            //no model name for this
            var content = aiCallInformation.IncomingCallDetails.RequestContent!.DeepClone();
            content["model"] = mappedModelName;
            return new StringContent(content.ToString(), Encoding.UTF8, "application/json");
        }

        if (aiCallInformation.IncomingCallDetails.AICallType == AICallType.Transcription ||
            aiCallInformation.IncomingCallDetails.AICallType == AICallType.Translation)
        {
            incomingRequest.Body.Position = 0;
            var newContent = new MultipartFormDataContent(incomingRequest.GetMultipartBoundary());
            var incomingRequestMultipart = new MultipartReader(incomingRequest.GetMultipartBoundary(), incomingRequest.Body);

            while (await incomingRequestMultipart.ReadNextSectionAsync() is { } nextSection)
            {
                var fileSection = nextSection.AsFileSection();
                if (fileSection != null)
                {
                    var ms = new MemoryStream();
                    await fileSection.FileStream!.CopyToAsync(ms);
                    ms.Position = 0;
                    var fileContent = new StreamContent(ms);
                    newContent.Add(fileContent, fileSection.Name, fileSection.FileName);
                }
                var formSection = nextSection.AsFormDataSection();
                if (formSection != null)
                {
                    var contentType = new ContentType(nextSection.ContentType ?? "text/plain; charset=utf-8");
                    newContent.Add(
                        new StringContent(formSection.Name == "model" ? mappedModelName! : await formSection.GetValueAsync(), Encoding.GetEncoding(contentType.CharSet ?? "utf-8"), contentType.MediaType),
                        formSection.Name);
                }

            }
            return newContent;
        }
        
        return aiCallInformation.IncomingCallDetails.IncomingModelName == mappedModelName
            ? new StringContent(aiCallInformation.IncomingCallDetails.RequestContent!.ToString(), Encoding.UTF8,
                "application/json")
            : new StringContent(
                AddModelName(aiCallInformation.IncomingCallDetails.RequestContent!.DeepClone(), mappedModelName!)
                    .ToString(),
                Encoding.UTF8, "application/json");

    }

    private static JToken AddModelName(JToken deepClone, string mappedModelName)
    {
        deepClone["model"] = mappedModelName;
        return deepClone;
    }
}
