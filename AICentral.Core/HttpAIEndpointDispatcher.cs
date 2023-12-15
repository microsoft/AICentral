using System.Net;

namespace AICentral.Core;

/// <summary>
/// Registered as a Typed Http Client to leverage HttpClientFactory.
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

    public async Task<HttpResponseMessage> Dispatch(HttpRequestMessage outgoingRequest, CancellationToken cancellationToken)
    {
        if (_retryAt != null && _dateTimeProvider.Now < _retryAt)
        {
            _logger.LogDebug("Avoiding endpoint {Endpoint} as it rate limited us until {RetryAt}",
                outgoingRequest.RequestUri!.AbsoluteUri, _retryAt);
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        }

        _retryAt = null;
        _logger.LogDebug("Making call to {Endpoint}", outgoingRequest.RequestUri!.AbsoluteUri);

        //HttpCompletionOption.ResponseHeadersRead ensures we can get to streaming results much quicker.
        var response = await _httpClient.SendAsync(outgoingRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        _logger.LogDebug(
            "Called {Endpoint}. Response Code: {ResponseCode}",
            outgoingRequest.RequestUri!.AbsoluteUri,
            response.StatusCode);

        return response;
    }
}