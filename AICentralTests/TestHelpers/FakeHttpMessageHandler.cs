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
        if (request.RequestUri.Host.Equals(AICentralFakeResponses.Endpoint200Image))
        {
            return Task.FromResult(AICentralFakeResponses.FakeImageResponse());
        }
        
        if (request.RequestUri!.Host.Equals(AICentralFakeResponses.Endpoint404))
        {
            return Task.FromResult(AICentralFakeResponses.NotFoundResponse());
        }

        if (request.RequestUri!.Host.Equals(AICentralFakeResponses.Endpoint500))
        {
            return Task.FromResult(AICentralFakeResponses.InternalServerErrorResponse());
        }
        return Task.FromResult(_fakeResponse);
    }
}