namespace AICentralTests.TestHelpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly FakeHttpMessageHandlerSeeder _seeder;

    public FakeHttpMessageHandler(FakeHttpMessageHandlerSeeder seeder)
    {
        _seeder = seeder;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_seeder.SeededResponses.TryGetValue(request.RequestUri!.AbsoluteUri, out var response))
        {
            return response();
        }

        throw new NotSupportedException($"No fake response registered for {request.RequestUri.AbsoluteUri}");
    }
}