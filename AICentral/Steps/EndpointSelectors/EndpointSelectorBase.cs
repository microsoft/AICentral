using System.Net;
using Microsoft.DeepDev;
using Microsoft.Extensions.Primitives;

namespace AICentral.Steps.EndpointSelectors;

public abstract class EndpointSelectorBase : IEndpointSelector
{
    private static readonly string[] HeaderPrefixesToCopy = { "x-", "apim" };

    private static readonly Dictionary<string, ITokenizer> Tokenisers = new()
    {
        ["gpt-3.5-turbo-0613"] = TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo").Result,
        ["gpt-35-turbo"] = TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo").Result,
        ["gpt-4"] = TokenizerBuilder.CreateByModelNameAsync("gpt-4").Result,
    };

    /// <param name="logger"></param>
    /// <param name="context"></param>
    /// <param name="requestInformation"></param>
    /// <param name="openAiResponse"></param>
    /// <param name="lastChanceMustHandle">Used if you have no more servers to try. When this happens we will proxy back whatever response we can.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task<AICentralResponse> HandleResponse(
        ILogger logger,
        HttpContext context,
        AICentralRequestInformation requestInformation,
        HttpResponseMessage openAiResponse,
        bool lastChanceMustHandle,
        CancellationToken cancellationToken)
    {
        if (openAiResponse.StatusCode == HttpStatusCode.OK)
        {
            context.Response.Headers.TryAdd("x-aicentral-server", new StringValues(requestInformation.LanguageUrl));
        }
        else
        {
            if (context.Response.Headers.TryGetValue("x-aicentral-failed-servers", out var header))
            {
                context.Response.Headers.Remove("x-aicentral-failed-servers");
            }
            context.Response.Headers.TryAdd("x-aicentral-failed-servers", StringValues.Concat(header, requestInformation.LanguageUrl));
        }

        //Now blow up if we didn't succeed
        if (!lastChanceMustHandle)
        {
            openAiResponse.EnsureSuccessStatusCode();
        }

        CopyHeadersToResponse(context, openAiResponse);

        if (openAiResponse.Headers.TransferEncodingChunked == true)
        {
            logger.LogDebug("Detected chunked encoding response. Streaming response back to consumer");
            return await ServerSideEventResponseHandler.Handle(Tokenisers, context, cancellationToken, openAiResponse, requestInformation);
        }

        logger.LogDebug("Detected non-chunked encoding response. Sending response back to consumer");
        return await JsonResponseHandler.Handle(
            context,
            cancellationToken,
            openAiResponse,
            requestInformation);
    }

    private static void CopyHeadersToResponse(HttpContext context, HttpResponseMessage openAiResponse)
    {
        foreach (var header in
                 openAiResponse.Headers.Where(x =>
                     HeaderPrefixesToCopy.Any(p => x.Key.StartsWith(p, StringComparison.InvariantCultureIgnoreCase))))
        {
            context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
        }
    }

    public abstract object WriteDebug();

    public abstract Task<AICentralResponse> Handle(
        HttpContext context, 
        AICallInformation aiCallInformation,
        AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken);
}