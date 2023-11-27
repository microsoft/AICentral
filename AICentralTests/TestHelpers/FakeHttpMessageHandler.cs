using System.Net;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentralTests.TestHelpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly FakeHttpMessageHandlerSeeder _seeder;
    private long _bulkHeadCount;

    public FakeHttpMessageHandler(FakeHttpMessageHandlerSeeder seeder)
    {
        _seeder = seeder;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_seeder.SeededResponses.TryGetValue(request.RequestUri!.AbsoluteUri, out var response))
        {
            return await response();
        }

        throw new NotSupportedException($"No fake response registered for {request.RequestUri.AbsoluteUri}");
    }
}