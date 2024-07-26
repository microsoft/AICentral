namespace AICentral.Core;

public interface IResponseGenerator
{
    Task<AICentralResponse> BuildResponse(
        DownstreamRequestInformation downstreamRequestInformation,
        IRequestContext context,
        HttpResponseMessage rawResponse,
        ResponseMetadata responseMetadata,
        CancellationToken cancellationToken);
}