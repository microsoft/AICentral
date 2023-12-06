using System.Net;
using AICentral.Core;
using Microsoft.DeepDev;
using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.EndpointSelectors;

public abstract class EndpointSelectorBase : IEndpointSelector
{
    private static readonly Dictionary<string, ITokenizer> Tokenisers = new()
    {
        ["gpt-3.5-turbo-0613"] = TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo").Result,
        ["gpt-35-turbo"] = TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo").Result,
        ["gpt-4"] = TokenizerBuilder.CreateByModelNameAsync("gpt-4").Result,
    };

    /// <param name="logger"></param>
    /// <param name="context"></param>
    /// <param name="openAiResponse"></param>
    /// <param name="lastChanceMustHandle"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<AICentralResponse> HandleResponse(
        ILogger logger, 
        HttpContext context, 
        (AICentralRequestInformation RequestInformation, HttpResponseMessage RawResponseMessage, Dictionary<string, StringValues> SanistisedHeaders) openAiResponse, 
        bool lastChanceMustHandle, 
        CancellationToken cancellationToken)
    {
        if (openAiResponse.RawResponseMessage.StatusCode == HttpStatusCode.OK)
        {
            context.Response.Headers.TryAdd("x-aicentral-server", new StringValues(openAiResponse.RequestInformation.LanguageUrl));
        }
        else
        {
            if (context.Response.Headers.TryGetValue("x-aicentral-failed-servers", out var header))
            {
                context.Response.Headers.Remove("x-aicentral-failed-servers");
            }

            context.Response.Headers.TryAdd("x-aicentral-failed-servers",
                StringValues.Concat(header, openAiResponse.RequestInformation.LanguageUrl));
        }

        //Now blow up if we didn't succeed
        if (!lastChanceMustHandle)
        {
            openAiResponse.RawResponseMessage.EnsureSuccessStatusCode();
        }

        CopyHeadersToResponse(context.Response, openAiResponse.SanistisedHeaders);

        if (openAiResponse.RawResponseMessage.Headers.TransferEncodingChunked == true)
        {
            logger.LogDebug("Detected chunked encoding response. Streaming response back to consumer");
            return await ServerSideEventResponseHandler.Handle(
                Tokenisers, 
                context, 
                cancellationToken, 
                openAiResponse.RawResponseMessage,
                openAiResponse.RequestInformation);
        }

        if ((openAiResponse.RawResponseMessage.Content.Headers.ContentType?.MediaType ?? string.Empty).Contains("json",
                StringComparison.InvariantCultureIgnoreCase))
        {
            logger.LogDebug("Detected non-chunked encoding response. Sending response back to consumer");
            return await JsonResponseHandler.Handle(
                context,
                cancellationToken,
                openAiResponse.RawResponseMessage,
                openAiResponse.RequestInformation);
        }

        return await StreamResponseHandler.Handle(
            context,
            cancellationToken,
            openAiResponse.RawResponseMessage,
            openAiResponse.RequestInformation);
    }

    private static void CopyHeadersToResponse(HttpResponse response, Dictionary<string, StringValues> headersToProxy)
    {
        foreach (var header in headersToProxy)
        {
            response.Headers.TryAdd(header.Key, header.Value);
        }
    }

    public abstract Task<AICentralResponse> Handle(
        HttpContext context,
        AICallInformation aiCallInformation,
        bool isLastChance,
        CancellationToken cancellationToken);
}