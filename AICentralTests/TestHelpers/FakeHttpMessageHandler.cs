namespace AICentralTests.TestHelpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _fakeResponse;

    public FakeHttpMessageHandler(HttpResponseMessage fakeResponse)
    {
        _fakeResponse = fakeResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri!.Host.Equals(AICentralTestEndpointBuilder.Endpoint404))
        {
            return Task.FromResult(AICentralTestEndpointBuilder.NotFoundResponse());
        }
        return Task.FromResult(_fakeResponse);
    }
}