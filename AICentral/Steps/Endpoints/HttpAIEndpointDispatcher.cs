using System.Web;

namespace AICentral.Steps.Endpoints;

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

    public async Task<HttpResponseMessage> Dispatch(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Making call to {Endpoint}", request.RequestUri!.AbsoluteUri);

        //HttpCompletionOption.ResponseHeadersRead ensures we can get to streaming results much quicker.
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        _logger.LogDebug(
            "Called {Endpoint}. Response Code: {ResponseCode}", 
            request.RequestUri!.AbsoluteUri, 
            response.StatusCode);

        return response;
    }
}
