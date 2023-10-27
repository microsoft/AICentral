using System.Text;
using AICentral.Pipelines.Endpoints.EndpointAuth;
using Polly;

namespace AICentral.Pipelines.Endpoints;

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
            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Made successful call to {Endpoint}", endpointUrl);
            return response;
        }, new Uri(endpointUrl), cancellationToken);
    }
}