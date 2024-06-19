using System.Net;
using System.Text;
using AICentralOpenAIMock;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Newtonsoft.Json;
using OpenAIMock;
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
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint500, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.InternalServerErrorResponse()));
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint404, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.NotFoundResponse()));
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse()));

        var result = await _httpClient
            .PostAsync($"http://azure-noauth-priority.localtest.me/openai/deployments/Model1/chat/completions?api-version={OpenAITestEx.OpenAIClientApiVersion}",
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
        
        result.Headers.GetValues("x-aicentral-failed-servers").ShouldContain(TestPipelines.Endpoint404);
        result.Headers.GetValues("x-aicentral-failed-servers").ShouldContain(TestPipelines.Endpoint500);

        result.Headers.GetValues("x-aicentral-server").Single().ShouldBe(TestPipelines.Endpoint200);
    }

    public void Dispose()
    {
        _factory.Services.ClearSeededMessages();
    }
}