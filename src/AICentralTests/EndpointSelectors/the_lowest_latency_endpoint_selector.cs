using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.EndpointSelectors;

public class the_lowest_latency_endpoint_selector : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_lowest_latency_endpoint_selector(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task finds_the_lowest_latency_endpoint()
    {
        var rnd = new Random(Environment.TickCount);
        _factory.SeedChatCompletions(AICentralFakeResponses.FastEndpoint, "random", async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(rnd.Next(0, 5)));
                return AICentralFakeResponses.FakeChatCompletionsResponse();
            });
        _factory.SeedChatCompletions(AICentralFakeResponses.SlowEndpoint, "random", async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(rnd.Next(25, 60)));
                return AICentralFakeResponses.FakeChatCompletionsResponse();
            });

        var results = await Task.WhenAll(Enumerable.Range(0, 100).Select(async _ =>
        {
            await Task.Delay(rnd.Next(0, 25));
            return await _httpClient.PostAsync(
                $"http://lowest-latency-tester.localtest.me/openai/deployments/random/chat/completions?api-version={AICentralTestEx.OpenAIClientApiVersion}",
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

        fastEndpointCount.ShouldBeInRange(70, 100);
        slowEndpointCount.ShouldBeInRange(0, 30);
    }
}