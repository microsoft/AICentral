using AICentral.Core;
using Microsoft.AspNetCore.Http.Features;

namespace AICentral.ResultHandlers;

public class StreamResponseHandler
{
    public static async Task<AICentralResponse> Handle(
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        DownstreamRequestInformation requestInformation,
        ResponseMetadata responseMetadata)
    {
        //send the headers down to the client
        context.Response.StatusCode = (int)openAiResponse.StatusCode;
        context.Response.Headers.ContentType = openAiResponse.Content.Headers.ContentType?.ToString();

        //squirt the response as it comes in:
        await openAiResponse.Content.CopyToAsync(context.Response.Body, cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);

        var chatRequestInformation = new DownstreamUsageInformation(
            requestInformation.LanguageUrl,
            requestInformation.InternalEndpointName,
            null,
            requestInformation.DeploymentName,
            context.GetClientForLoggingPurposes(),
            requestInformation.CallType,
            null,
            requestInformation.Prompt,
            null,
            null,
            null,
            responseMetadata,
            context.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ?? context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            requestInformation.StartDate,
            requestInformation.Duration,
            openAiResponse.IsSuccessStatusCode);

        return new AICentralResponse(chatRequestInformation, new StreamAlreadySentResultHandler());
    }
}