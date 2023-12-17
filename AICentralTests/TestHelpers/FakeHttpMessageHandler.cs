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
        if (_seeder.TryGet(request, out var response))
        {
            return Task.FromResult(response!);
        }

        throw new NotSupportedException($"No fake response registered for {request.RequestUri.AbsoluteUri}");
    }
}