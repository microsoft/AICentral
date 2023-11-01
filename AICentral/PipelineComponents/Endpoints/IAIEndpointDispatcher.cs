using AICentral.PipelineComponents.Endpoints.EndpointAuth;
using Polly;

namespace AICentral.PipelineComponents.Endpoints;

public interface IAIEndpointDispatcher
{
    Task<HttpResponseMessage> Dispatch(HttpClient httpClient, HttpContext context,
        ResiliencePipeline<HttpResponseMessage> retry, string endpointUrl, string requestRawContent,
        IEndpointAuthorisationHandler authHandler, CancellationToken cancellationToken);
}