using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using Microsoft.AspNetCore.Server.HttpSys;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests;

public class the_endpoint_dispatchers : IClassFixture<TestWebApplicationFactory<Program>>

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;
    private long _bulkHeadCount = 0;
    
    private async Task<HttpResponseMessage> BulkheadResponse(CancellationToken cancellationToken)
    {
        if (Interlocked.Read(ref _bulkHeadCount) == 5)
        {
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        }
    
        Interlocked.Increment(ref _bulkHeadCount);
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        Interlocked.Decrement(ref _bulkHeadCount);
        return AICentralFakeResponses.FakeCompletionsResponse();
    }

    public the_endpoint_dispatchers(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task can_buffer_requests_at_the_pipeline_layer_to_reduce_endpoint_pressure()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            () => BulkheadResponse(new CancellationToken()));

        var allResponses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => _httpClient.PostAsync(
            "https://azure-with-bulkhead.localtest.me/openai/deployments/Model1/chat/completions?api-version=2023-05-15",
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "Does Azure OpenAI support customer managed keys?" },
                },
                max_tokens = 5
            }), Encoding.UTF8, "application/json"))));

        allResponses.ShouldAllBe(x => x.StatusCode == HttpStatusCode.OK);
    }

    [Fact(Skip = "Polly operates deep in HttpClientHandler and my tests currently override the Message Handler")]
    public async Task can_buffer_requests_at_the_endpoint_layer_to_reduce_endpoint_pressure()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            () => BulkheadResponse(new CancellationToken()));

        var allResponses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => _httpClient.PostAsync(
            "https://azure-with-bulkhead-on-endpoint.localtest.me/openai/deployments/Model1/chat/completions?api-version=2023-05-15",
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "Does Azure OpenAI support customer managed keys?" },
                },
                max_tokens = 5
            }), Encoding.UTF8, "application/json"))));

        allResponses.ShouldAllBe(x => x.StatusCode == HttpStatusCode.OK);
    }
}