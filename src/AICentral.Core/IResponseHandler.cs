namespace AICentral.Core;

public interface IResponseHandler
{
    Task<AICentralResponse> Handle(IRequestContext context, CancellationToken cancellationToken, HttpResponseMessage openAiResponse, DownstreamRequestInformation requestInformation, ResponseMetadata responseMetadata);
}