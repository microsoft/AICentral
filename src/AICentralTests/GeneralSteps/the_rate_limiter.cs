using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.GeneralSteps;

public class the_rate_limiter : IClassFixture<TestWebApplicationFactory<Program>>

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public the_rate_limiter(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        //factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task rate_limits()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        Task<HttpResponseMessage> Call() => _httpClient.PostAsync(
            "http://azure-with-rate-limiter.localtest.me/openai/deployments/Model1/chat/completions?api-version=2023-12-01-preview",
            new StringContent(
                JsonConvert.SerializeObject(new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant." },
                        new { role = "user", content = "Does Azure OpenAI support customer managed keys?" },
                        new
                        {
                            role = "assistant", content = "Yes, customer managed keys are supported by Azure OpenAI."
                        },
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
                "http://azure-with-client-partitioned-rate-limiter.localtest.me/openai/deployments/Model1/chat/completions?api-version=2023-12-01-preview")
            {
                Headers = { { "api-key", apiKey } },
                Content = new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        messages = new[]
                        {
                            new { role = "system", content = "You are a helpful assistant." },
                        },
                        max_tokens = 5
                    }), Encoding.UTF8, "application/json")
            });

        //takes a while for the tokenisers to spin up, which happens after the rate-limiter has registered a request
        await Call("123");
        await Task.Delay(TimeSpan.FromSeconds(2));

        _testOutputHelper.WriteLine("1");
        var client1Call1 = await Call("ignore-fake-key-123");
        _testOutputHelper.WriteLine("2");
        var client2Call1 = await Call("ignore-fake-key-456");
        _testOutputHelper.WriteLine("3");
        var client1Call2 = await Call("ignore-fake-key-123");
        _testOutputHelper.WriteLine("4");
        var client2Call2 = await Call("ignore-fake-key-456");

        client1Call1.StatusCode.ShouldBe(HttpStatusCode.OK);
        client1Call2.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        client2Call1.StatusCode.ShouldBe(HttpStatusCode.OK);
        client2Call2.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        
        await Task.Delay(TimeSpan.FromSeconds(3));
        
        client1Call1 = await Call("ignore-fake-key-123");
        client2Call1 = await Call("ignore-fake-key-456");
        client1Call2 = await Call("ignore-fake-key-123");
        client2Call2 = await Call("ignore-fake-key-456");

        client1Call1.StatusCode.ShouldBe(HttpStatusCode.OK);
        client1Call2.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        client2Call1.StatusCode.ShouldBe(HttpStatusCode.OK);
        client2Call2.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

    }
}