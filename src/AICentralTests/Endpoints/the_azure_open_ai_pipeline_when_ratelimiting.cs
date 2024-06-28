using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class the_azure_open_ai_pipeline_when_ratelimiting : IClassFixture<TestWebApplicationFactory<Program>>,
    IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;
    private readonly OpenAIClient _client;

    public the_azure_open_ai_pipeline_when_ratelimiting(TestWebApplicationFactory<Program> factory,
        ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();

        _client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });
    }

    [Fact]
    public async Task handles_the_odd_day_long_retry_after()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.RateLimitResponse(TimeSpan.FromDays(1))));

        (await Should.ThrowAsync<RequestFailedException>(() => _client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                DeploymentName = "Model1",
                Messages = { new ChatRequestSystemMessage("Test") }
            }))).Status.ShouldBe(429);
        
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.RateLimitResponse(TimeSpan.FromSeconds(5))));

        //instant retry should also throw a 429. But advance 5 seconds later and we should be good...
        (await Should.ThrowAsync<RequestFailedException>(() => _client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                DeploymentName = "Model1",
                Messages = { new ChatRequestSystemMessage("Test") }
            }))).Status.ShouldBe(429);

        var fakeDateTimeProvider = _factory.Services.GetRequiredService<FakeDateTimeProvider>();
        fakeDateTimeProvider.Advance(TimeSpan.FromSeconds(6));

        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse()));
        
        var response = await _client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                DeploymentName = "Model1",
                Messages = { new ChatRequestSystemMessage("Test") }
            });
        
        response.GetRawResponse().Status.ShouldBe(200);

    }

    public void Dispose()
    {
        _factory.Dispose();
        _httpClient.Dispose();
    }
}