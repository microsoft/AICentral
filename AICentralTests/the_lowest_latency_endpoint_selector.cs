using System.Text;
using AICentralTests.TestHelpers;
using Newtonsoft.Json;
using Shouldly;

namespace AICentralTests;

public class the_lowest_latency_endpoint_selector : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_lowest_latency_endpoint_selector(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task finds_the_lowest_latency_endpoint(int _)
    {
        var rnd = new Random(Environment.TickCount);
        _factory.SeedChatCompletions(AICentralFakeResponses.FastEndpoint, "Model1", async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(rnd.Next(0, 10)));
                return AICentralFakeResponses.FakeChatCompletionsResponse();
            });
        _factory.SeedChatCompletions(AICentralFakeResponses.SlowEndpoint, "Model1", async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(rnd.Next(50, 100)));
                return AICentralFakeResponses.FakeChatCompletionsResponse();
            });

        var results = await Task.WhenAll(Enumerable.Range(0, 2000).Select(async _ =>
        {
            await Task.Delay(rnd.Next(0, 1000));
            return await _httpClient.PostAsync(
                "http://lowest-latency-tester.localtest.me/openai/deployments/random/chat/completions?api-version=2023-05-15",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant." },
                    },
                    max_tokens = 5
                }), Encoding.UTF8, "application/json"));
        }));

        var slowEndpointCount = results.Count(x => x.Headers.GetValues("x-aicentral-server").Single().EndsWith(AICentralFakeResponses.SlowEndpoint));
        var fastEndpointCount = results.Count(x => x.Headers.GetValues("x-aicentral-server").Single().EndsWith(AICentralFakeResponses.FastEndpoint));

        fastEndpointCount.ShouldBeInRange(1300, 2000);
        slowEndpointCount.ShouldBeInRange(0, 700);
    }
}