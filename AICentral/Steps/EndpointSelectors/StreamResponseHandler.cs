using AICentral.Steps.Endpoints.ResultHandlers;

namespace AICentral.Steps.EndpointSelectors;

public class StreamResponseHandler
{
    public static async Task<AICentralResponse> Handle(
        HttpContext context,
        CancellationToken cancellationToken,
        HttpResponseMessage openAiResponse,
        AICentralRequestInformation requestInformation)
    {
        //send the headers down to the client
        context.Response.StatusCode = (int)openAiResponse.StatusCode;
        context.Response.Headers.ContentType = openAiResponse.Content.Headers.ContentType?.ToString();

        //squirt the response as it comes in:
        await openAiResponse.Content.CopyToAsync(context.Response.Body, cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);

        var chatRequestInformation = new AICentralUsageInformation(
            requestInformation.LanguageUrl,
            null,
            context.User.Identity?.Name ?? "unknown",
            requestInformation.CallType,
            requestInformation.Prompt,
            null,
            null,
            null,
            null,
            null,
            null,
            context.Connection.RemoteIpAddress?.ToString() ?? "",
            requestInformation.StartDate,
            requestInformation.Duration);

        return new AICentralResponse(chatRequestInformation, new StreamAlreadySentResultHandler());
    }
}