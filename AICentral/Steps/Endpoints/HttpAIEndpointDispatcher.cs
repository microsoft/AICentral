using System.Net;
using AICentral.Steps.TokenBasedRateLimiting;

namespace AICentral.Steps.Endpoints;

/// <summary>
/// Registered as a Typed Http Client to leverage HttpClientFactory. Created with an IAIEndpointDispatcher to allow a fake for testing purposes
/// </summary>
public class HttpAIEndpointDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<HttpAIEndpointDispatcher> _logger;
    private DateTimeOffset? _retryAt;

    public HttpAIEndpointDispatcher(
        HttpClient httpClient,
        IDateTimeProvider dateTimeProvider,
        ILogger<HttpAIEndpointDispatcher> logger)
    {
        _httpClient = httpClient;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> Dispatch(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_retryAt != null && _dateTimeProvider.Now < _retryAt)
        {
            _logger.LogDebug("Avoiding endpoint {Endpoint} as it rate limited us until {RetryAt}",
                request.RequestUri!.AbsoluteUri, _retryAt);
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        }

        _retryAt = null;
        _logger.LogDebug("Making call to {Endpoint}", request.RequestUri!.AbsoluteUri);

        //HttpCompletionOption.ResponseHeadersRead ensures we can get to streaming results much quicker.
        var response =
            await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        _logger.LogDebug(
            "Called {Endpoint}. Response Code: {ResponseCode}",
            request.RequestUri!.AbsoluteUri,
            response.StatusCode);

        return response;
    }
}