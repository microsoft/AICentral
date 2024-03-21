using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AICentral.Core;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Primitives;

namespace AICentral.Endpoints;

public abstract class OpenAILikeDownstreamEndpointAdapter : IDownstreamEndpointAdapter
{
    public string Id { get; }
    public Uri BaseUrl { get; }
    public string EndpointName { get; }
    protected abstract string[] HeadersToIgnore { get; }
    protected abstract string[] HeaderPrefixesToCopy { get; }
    private readonly Dictionary<string, string> _modelMappings;
    private readonly Dictionary<string, string> _assistantMappings;

    protected OpenAILikeDownstreamEndpointAdapter(string id, Uri baseUrl, string endpointName,
        Dictionary<string, string> modelMappings, Dictionary<string, string> assistantMappings)
    {
        _modelMappings = modelMappings;
        _assistantMappings = assistantMappings;
        Id = id;
        BaseUrl = baseUrl;
        EndpointName = endpointName;
    }

    public async Task<Either<HttpRequestMessage, IResult>> BuildRequest(IncomingCallDetails callInformation,
        HttpContext context)
    {
        var incomingModelName = callInformation.IncomingModelName;
        _modelMappings.TryGetValue(incomingModelName ?? string.Empty, out var mappedModelName);

        var incomingAssistantName = callInformation.IncomingAssistantName;
        _assistantMappings.TryGetValue(incomingAssistantName ?? string.Empty, out var mappedAssistantName);
        mappedAssistantName ??=
            incomingAssistantName; //We are not transforming outgoing responses (i.e. changing the id of the agent to the friendly one)... so we need to keep the original name

        if (mappedModelName == null && IsFixedModelName(callInformation.AICallType, callInformation.IncomingModelName,
                out var fixedModelName))
        {
            mappedModelName = fixedModelName;
        }

        if (callInformation.IncomingModelName != null && string.IsNullOrWhiteSpace(mappedModelName))
        {
            return new Either<HttpRequestMessage, IResult>(Results.NotFound(new { message = "Unknown model mapping" }));
        }

        try
        {
            return new Either<HttpRequestMessage, IResult>(
                await BuildNewRequest(
                    context,
                    callInformation,
                    mappedModelName,
                    mappedAssistantName));
        }
        catch (InvalidOperationException ie)
        {
            return new Either<HttpRequestMessage, IResult>(Results.BadRequest(new { message = ie.Message }));
        }
    }

    public async Task<HttpResponseMessage> DispatchRequest(HttpContext context, HttpRequestMessage requestMessage,
        CancellationToken cancellationToken)
    {
        var typedDispatcher = context.RequestServices
            .GetRequiredService<ITypedHttpClientFactory<HttpAIEndpointDispatcher>>()
            .CreateClient(
                context.RequestServices.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(Id)
            );

        return await typedDispatcher.Dispatch(requestMessage, cancellationToken);
    }

    public Task<ResponseMetadata> ExtractResponseMetadata(
        IncomingCallDetails callInformationIncomingCallDetails,
        HttpContext context,
        HttpResponseMessage openAiResponse)
    {
        openAiResponse.Headers.TryGetValues("x-ratelimit-remaining-requests", out var remainingRequestHeaderValues);
        openAiResponse.Headers.TryGetValues("x-ratelimit-remaining-tokens", out var remainingTokensHeaderValues);
        var didHaveRequestLimitHeader =
            long.TryParse(remainingRequestHeaderValues?.FirstOrDefault(), out var remainingRequests);
        var didHaveTokenLimitHeader =
            long.TryParse(remainingTokensHeaderValues?.FirstOrDefault(), out var remainingTokens);

        return Task.FromResult(new ResponseMetadata(
            SanitiseHeaders(context, openAiResponse),
            didHaveTokenLimitHeader ? remainingTokens : null,
            didHaveRequestLimitHeader ? remainingRequests : null));
    }

    private Dictionary<string, StringValues> SanitiseHeaders(HttpContext context, HttpResponseMessage openAiResponse)
    {
        var proxiedHeaders = new Dictionary<string, StringValues>();
        foreach (var header in openAiResponse.Headers)
        {
            if (HeaderPrefixesToCopy.Any(x => header.Key.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                proxiedHeaders.Add(header.Key, new StringValues(header.Value.ToArray()));
            }
        }

        CustomSanitiseHeaders(context, openAiResponse, proxiedHeaders);

        return proxiedHeaders;
    }

    protected virtual void CustomSanitiseHeaders(
        HttpContext context,
        HttpResponseMessage openAiResponse,
        Dictionary<string, StringValues> proxiedHeaders)
    {
    }

    /// <summary>
    /// If we didn't find a mapped-model name then you have an option to pin it to something.
    /// </summary>
    /// <remarks>
    /// For AOAI you might just pass through the original model name, but for OpenAI you might want to map it to a specific model name.
    /// </remarks>
    protected abstract bool IsFixedModelName(AICallType callType, string? callInformationIncomingModelName,
        out string? fixedModelName);

    private async Task<HttpRequestMessage> BuildNewRequest(
        HttpContext context,
        IncomingCallDetails callInformation,
        string? mappedModelName,
        string? mappedAssistantName)
    {
        var newRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method),
            BuildUri(context, callInformation, callInformation.IncomingAssistantName, mappedAssistantName,
                callInformation.IncomingModelName, mappedModelName));

        foreach (var header in context.Request.Headers)
        {
            if (HeadersToIgnore.Contains(header.Key.ToLowerInvariant())) continue;

            if (!newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) &&
                newRequest.Content != null)
            {
                newRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        await CustomiseRequest(context, callInformation, newRequest, mappedModelName, mappedAssistantName);

        return newRequest;
    }


    private Task<HttpContent?> CopyRequestWithMappedModelAndAssistantName(
        IncomingCallDetails aiCallInformation,
        HttpRequest incomingRequest,
        string? mappedModelName,
        string? mappedAssistantName
    )
    {
        if (aiCallInformation.AICallType == AICallType.Transcription ||
            aiCallInformation.AICallType == AICallType.Translation ||
            aiCallInformation.AICallType == AICallType.Files)
        {
            return Task.FromResult<HttpContent?>(
                MultipartContentHelper.CopyMultipartContent(incomingRequest, mappedModelName));
        }

        if (aiCallInformation.RequestContent == null)
        {
            return Task.FromResult<HttpContent?>(null);
        }

        if (aiCallInformation.IncomingModelName != mappedModelName ||
            aiCallInformation.IncomingAssistantName != mappedAssistantName)
        {
            return Task.FromResult<HttpContent?>(
                new StringContent(
                    JsonSerializer.Serialize(
                        AddModelAndAssistantName(
                            aiCallInformation.RequestContent!.Deserialize<JsonNode>()!.DeepClone(),
                            mappedModelName,
                            mappedAssistantName)),
                    Encoding.UTF8, "application/json"));
        }

        return Task.FromResult<HttpContent?>(new StringContent(
            JsonSerializer.Serialize(aiCallInformation.RequestContent),
            Encoding.UTF8,
            "application/json"));
    }


    /// <summary>
    /// Adjust the model name to the one used by Open AI
    /// </summary>
    /// <param name="deepClone"></param>
    /// <param name="mappedModelName"></param>
    /// <param name="mappedAssistantName"></param>
    /// <returns></returns>
    private JsonNode AddModelAndAssistantName(
        JsonNode deepClone,
        string? mappedModelName,
        string? mappedAssistantName)
    {
        if (mappedModelName != null)
        {
            if (deepClone["model"] != null)
            {
                deepClone["model"] = mappedModelName;
            }
            else if (ForceAddModelNameToBody())
            {
                deepClone["model"] = mappedModelName;
            }
        }

        if (mappedAssistantName != null)
        {
            deepClone["assistant_id"] = mappedAssistantName;
        }

        return deepClone;
    }

    private async Task CustomiseRequest(
        HttpContext context,
        IncomingCallDetails aiCallInformation,
        HttpRequestMessage newRequest,
        string? mappedModelName,
        string? mappedAssistantName)
    {
        if (aiCallInformation.AICallType == AICallType.Other)
        {
            //Byte for byte copy as we don't know enough to get any smarter
            newRequest.Content = new StreamContent(context.Request.Body);
            if (context.Request.Headers.ContentType.Count != 0)
            {
                newRequest.Content.Headers.Add("Content-Type", context.Request.Headers.ContentType.ToString());
            }
        }
        else
        {
            newRequest.Content = await CopyRequestWithMappedModelAndAssistantName(
                aiCallInformation, context.Request,
                mappedModelName, mappedAssistantName);
        }

        await ApplyAuthorisation(context, newRequest);
    }

    protected virtual bool ForceAddModelNameToBody() => false;

    protected abstract Task ApplyAuthorisation(HttpContext context, HttpRequestMessage newRequest);

    protected abstract string BuildUri(
        HttpContext context,
        IncomingCallDetails aiCallInformation,
        string? incomingAssistantName,
        string? mappedAssistantName,
        string? incomingModelName,
        string? mappedModelName);
}