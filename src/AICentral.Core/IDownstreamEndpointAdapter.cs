namespace AICentral.Core;

/// <summary>
/// Represents a downstream AI service that can have requests routed to it.
/// </summary>
public interface IDownstreamEndpointAdapter
{
    string Id { get; }
    string BaseUrl { get; }
    string EndpointName { get; }

    /// <summary>
    /// Given an incoming call, build a request to send to the AI service. If you cannot handle it return an IResult.
    /// </summary>
    /// <param name="incomingCall"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    Task<Either<AIRequest, IResult>> BuildRequest(IncomingCallDetails incomingCall, HttpContext context);

    /// <summary>
    /// PreProcess the response from the AI service. This is where you can do things like sanitise headers, or extract remaining tokens and requests.
    /// </summary>
    /// <param name="callInformationIncomingCallDetails"></param>
    /// <param name="context"></param>
    /// <param name="newRequest"></param>
    /// <param name="openAiResponse"></param>
    /// <returns></returns>
    Task<ResponseMetadata> ExtractResponseMetadata(
        IncomingCallDetails callInformationIncomingCallDetails,
        HttpContext context,
        AIRequest newRequest,
        HttpResponseMessage openAiResponse);
}