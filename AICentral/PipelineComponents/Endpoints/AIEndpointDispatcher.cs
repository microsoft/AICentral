using System.Text;
using AICentral.PipelineComponents.Endpoints.EndpointAuth;
using Polly;

namespace AICentral.PipelineComponents.Endpoints;

public class AIEndpointDispatcher : IAIEndpointDispatcher
{
    private readonly ILogger<AIEndpointDispatcher> _logger;

    public AIEndpointDispatcher(ILogger<AIEndpointDispatcher> logger)
    {
        _logger = logger;
    }

    public async Task<HttpResponseMessage> Dispatch(HttpClient httpClient, HttpContext context,
        ResiliencePipeline<HttpResponseMessage> retry, string endpointUrl, string requestRawContent,
        IEndpointAuthorisationHandler authHandler, CancellationToken cancellationToken)
    {
        return await retry.ExecuteAsync(async (state, _) =>
        {
            _logger.LogDebug("Making call to {Endpoint}", endpointUrl);
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, state)
            {
                Content = new StringContent(requestRawContent, Encoding.UTF8, "application/json")
            };
            
            await authHandler.ApplyAuthorisationToRequest(context.Request, httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Called {Endpoint}. Response Code: {ResponseCode}", endpointUrl, response.StatusCode);
            return response;
        }, new Uri(endpointUrl), cancellationToken);
    }
}