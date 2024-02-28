using AICentral.Core;

namespace LoadTest;

public class PreCannedEndpoint: IDownstreamEndpointAdapter, IDownstreamEndpointAdapterFactory
{
    public Task<Either<HttpRequestMessage, IResult>> BuildRequest(IncomingCallDetails incomingCall, HttpContext context)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> DispatchRequest(HttpResponseMessage requestMessage)
    {
        throw new NotImplementedException();
    }

    public Task<ResponseMetadata> ExtractResponseMetadata(IncomingCallDetails callInformationIncomingCallDetails, HttpContext context,
        HttpResponseMessage openAiResponse)
    {
        throw new NotImplementedException();
    }

    public string Id { get; }
    public Uri BaseUrl { get; }
    public string EndpointName { get; }
    public void RegisterServices(HttpMessageHandler? httpMessageHandler, IServiceCollection services)
    {
        throw new NotImplementedException();
    }

    public IDownstreamEndpointAdapter Build()
    {
        throw new NotImplementedException();
    }

    public object WriteDebug()
    {
        throw new NotImplementedException();
    }
}