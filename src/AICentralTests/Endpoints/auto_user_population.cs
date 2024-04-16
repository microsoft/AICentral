using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class auto_user_population : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public auto_user_population(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task can_add_user_id()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "mapped",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-with-auto-populate-user.localtest.me"),
            new AzureKeyCredential("ignore-fake-key-123"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var result = await client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                Messages = { new ChatRequestSystemMessage("Hello") },
                DeploymentName = "mapped"
            });

        result.GetRawResponse().Status.ShouldBe(200);
        await Verify(_factory.VerifyRequestsAndResponses(result.GetRawResponse(), true));
    }

    [Fact]
    public async Task will_not_overwrite_existing_user_id()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "mapped",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-with-auto-populate-user.localtest.me"),
            new AzureKeyCredential("ignore-fake-key-123"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var result = await client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                Messages = { new ChatRequestSystemMessage("Hello") },
                DeploymentName = "mapped",
                User = "do-not-overwrite"
            });

        result.GetRawResponse().Status.ShouldBe(200);
        await Verify(_factory.VerifyRequestsAndResponses(result.GetRawResponse(), true));
    }

    public void Dispose()
    {
        _factory.Clear();
    }
}