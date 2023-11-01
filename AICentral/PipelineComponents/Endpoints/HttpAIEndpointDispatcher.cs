using AICentral.PipelineComponents.Endpoints.EndpointAuth;
using Polly;

namespace AICentral.PipelineComponents.Endpoints;

/// <summary>
/// Registered as a Typed Http Client to leverage HttpClientFactory. Created with an IAIEndpointDispatcher to allow a fake for testing purposes
/// </summary>
public class HttpAIEndpointDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly IAIEndpointDispatcher _innerDispatcher;

    public HttpAIEndpointDispatcher(HttpClient httpClient,
        IAIEndpointDispatcher innerDispatcher)
    {
        _httpClient = httpClient;
        _innerDispatcher = innerDispatcher;
    }

    public Task<HttpResponseMessage> Dispatch(HttpContext context, ResiliencePipeline<HttpResponseMessage> retry,
        string endpointUrl, string requestRawContent, IEndpointAuthorisationHandler authHandler,
        CancellationToken cancellationToken)
    {
        return _innerDispatcher.Dispatch(_httpClient, context, retry, endpointUrl, requestRawContent, authHandler,
            cancellationToken);
    }
}