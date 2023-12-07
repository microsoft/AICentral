using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AICentralTests.TestHelpers;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests;

public class the_token_rate_limiter : IClassFixture<TestWebApplicationFactory<Program>>

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_token_rate_limiter(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task rate_limits()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse(50)));

        Task<HttpResponseMessage> Call() => _httpClient.PostAsync(
            "http://azure-with-token-rate-limiter.localtest.me/openai/deployments/random/chat/completions?api-version=2023-05-15",
            new StringContent(
                JsonConvert.SerializeObject(new
                {
                    messages = new[]
                    {
                        new { role = "user", content = "Do other Azure AI services support this too?" }
                    },
                    max_tokens = 5
                }), Encoding.UTF8, "application/json"));

        var result = await Call();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        result = await Call();
        result.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task rate_limits_by_consumer()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        Task<HttpResponseMessage> Call(string apiKey) => _httpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Post,
                "http://azure-with-client-partitioned-token-rate-limiter.localtest.me/openai/deployments/random/chat/completions?api-version=2023-05-15")
            {
                Headers = { { "api-key", apiKey } },
                Content = new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        messages = new[]
                        {
                            new { role = "system", content = "You are a helpful assistant." },
                            new { role = "user", content = "Does Azure OpenAI support customer managed keys?" },
                            new
                            {
                                role = "assistant",
                                content = "Yes, customer managed keys are supported by Azure OpenAI."
                            },
                            new { role = "user", content = "Do other Azure AI services support this too?" }
                        },
                        max_tokens = 5
                    }), Encoding.UTF8, "application/json")
            });

        //takes a while for the tokenisers to spin up, which happens after the rate-limiter has registered a request
        await Call("123");
        await Task.Delay(TimeSpan.FromSeconds(1));

        var client1Call1 = await Call("123");
        var client2Call1 = await Call("456");
        var client1Call2 = await Call("123");
        var client2Call2 = await Call("456");

        client1Call1.StatusCode.ShouldBe(HttpStatusCode.OK);
        client1Call2.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        client2Call1.StatusCode.ShouldBe(HttpStatusCode.OK);
        client2Call2.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        
        await Task.Delay(TimeSpan.FromSeconds(3));
        
        client1Call1 = await Call("123");
        client2Call1 = await Call("456");
        client1Call2 = await Call("123");
        client2Call2 = await Call("456");

        client1Call1.StatusCode.ShouldBe(HttpStatusCode.OK);
        client1Call2.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        client2Call1.StatusCode.ShouldBe(HttpStatusCode.OK);
        client2Call2.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

    }
}