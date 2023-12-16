using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Newtonsoft.Json;
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
            "https://azure-with-auth.localtest.me/openai/deployments/api-key-auth/chat/completions?api-version=2023-05-15",
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
    [InlineData("123", true)]
    [InlineData("456", true)]
    [InlineData("789", false)]
    public async Task succeeds_with_correct_api_key(string apiKey, bool isValidKey)
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "api-key-auth",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://azure-with-auth.localtest.me/openai/deployments/api-key-auth/chat/completions?api-version=2023-05-15");
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