namespace AICentral.Core;

public interface IResponseGenerator
{
    Task<AICentralResponse> BuildResponse(
        DownstreamRequestInformation downstreamRequestInformation,
        HttpContext context,
        HttpResponseMessage rawResponse,
        ResponseMetadata responseMetadata,
        CancellationToken cancellationToken);
}