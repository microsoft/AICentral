using OpenAIMock;

namespace AICentralOpenAIMock;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly FakeHttpMessageHandlerSeeder _seeder;

    public FakeHttpMessageHandler(FakeHttpMessageHandlerSeeder seeder)
    {
        _seeder = seeder;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await _seeder.TryGet(request);
        if (response != null) return response;

        throw new NotSupportedException($"No fake response registered for {(request.RequestUri?.AbsoluteUri ?? "unknown url")}");
    }
}