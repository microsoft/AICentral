using System.Text;

namespace AICentral.PipelineComponents.Endpoints;

/// <summary>
/// Registered as a Typed Http Client to leverage HttpClientFactory. Created with an IAIEndpointDispatcher to allow a fake for testing purposes
/// </summary>
public class HttpAIEndpointDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpAIEndpointDispatcher> _logger;

    public HttpAIEndpointDispatcher(
        HttpClient httpClient,
        ILogger<HttpAIEndpointDispatcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> Dispatch(HttpContext context,
        string endpointUrl, string requestRawContent, IEndpointAuthorisationHandler authHandler,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Making call to {Endpoint}", endpointUrl);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(endpointUrl))
        {
            Content = new StringContent(requestRawContent, Encoding.UTF8, "application/json")
        };

        await authHandler.ApplyAuthorisationToRequest(context.Request, httpRequestMessage);

        //HttpCompletionOption.ResponseHeadersRead ensures we can get to streaming results much quicker.
        var response = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        _logger.LogDebug("Called {Endpoint}. Response Code: {ResponseCode}", endpointUrl, response.StatusCode);
        return response;
    }
}
