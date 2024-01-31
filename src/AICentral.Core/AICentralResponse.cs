namespace AICentral.Core;

/// <summary>
/// Represents an AI Central Response. The IResult represents the response to the end user. The DownstreamUsageInformation represents metadata about the request.
/// </summary>
/// <remarks>
/// For Streaming Calls AI Central will already have responded to the User by the time generic steps retrieve this. We try to minimise latency between the consumer and the response. This is represented by IResult being an instance of StreamAlreadySentResultHandler.
/// This will impact your ability to send leading HTTP Headers to the consumer, but you can still send trailing headers if supported.
/// </remarks>
/// <param name="DownstreamUsageInformation"></param>
/// <param name="ResultHandler"></param>
public record AICentralResponse(DownstreamUsageInformation DownstreamUsageInformation, IResult ResultHandler);
