using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.EndpointSelectors;

public class the_prioritised_endpoint_picker : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_prioritised_endpoint_picker(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task fails_over_to_a_successful_endpoint()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint500, "Model1",
            () => Task.FromResult(AICentralFakeResponses.InternalServerErrorResponse()));
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint404, "Model1",
            () => Task.FromResult(AICentralFakeResponses.NotFoundResponse()));
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var result = await _httpClient
            .PostAsync("http://azure-noauth-priority.localtest.me/openai/deployments/Model1/chat/completions?api-version=2023-05-15",
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "Does Azure OpenAI support customer managed keys?" },
                    new { role = "assistant", content = "Yes, customer managed keys are supported by Azure OpenAI." },
                    new { role = "user", content = "Do other Azure AI services support this too?" }
                },
                max_tokens = 5
            }), Encoding.UTF8, "application/json"));
        
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        result.Headers.GetValues("x-aicentral-failed-servers").ShouldContain($"https://{AICentralFakeResponses.Endpoint404}");
        result.Headers.GetValues("x-aicentral-failed-servers").ShouldContain($"https://{AICentralFakeResponses.Endpoint500}");

        result.Headers.GetValues("x-aicentral-server").Single().ShouldBe($"https://{AICentralFakeResponses.Endpoint200}");
    }

    public void Dispose()
    {
        _factory.Clear();
    }
}