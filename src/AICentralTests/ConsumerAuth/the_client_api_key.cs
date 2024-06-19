using System.Net;
using System.Text;
using AICentralOpenAIMock;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Newtonsoft.Json;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.ConsumerAuth;

public class the_client_api_key : IClassFixture<TestWebApplicationFactory<Program>>

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_client_api_key(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task fails_without_api_key()
    {
        var result = await _httpClient.PostAsync(
            "https://azure-with-auth.localtest.me/openai/deployments/api-key-auth/chat/completions?api-version=2024-02-15-preview",
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "user", content = "Do other Azure AI services support this too?" }
                },
                max_tokens = 5
            }), Encoding.UTF8, "application/json"));

        result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("ignore-fake-key-123", true)]
    [InlineData("ignore-fake-key-456", true)]
    [InlineData("ignore-fake-key-789", false)]
    public async Task succeeds_with_correct_api_key(string apiKey, bool isValidKey)
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "api-key-auth",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse()));

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://azure-with-auth.localtest.me/openai/deployments/api-key-auth/chat/completions?api-version={OpenAITestEx.OpenAIClientApiVersion}");
        request.Content =
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "user", content = "Do other Azure AI services support this too?" }
                },
                max_tokens = 5
            }), Encoding.UTF8, "application/json");

        request.Headers.Add("api-key", apiKey);

        var result = await _httpClient.SendAsync(request);

        if (isValidKey)
        {
            result.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        else
        {
            result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}