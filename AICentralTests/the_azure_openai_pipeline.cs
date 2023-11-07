using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using ApprovalTests;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests;

public class the_azure_openai_pipeline : IClassFixture<TestWebApplicationFactory<Program>>

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_azure_openai_pipeline(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task handles_chats()
    {
        var result = await _httpClient.PostAsync(
            "/openai/deployments/random/chat/completions?api-version=2023-05-15",
            new StringContent(JsonConvert.SerializeObject(new
            {
            }), Encoding.UTF8, "application/json"));

        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();
        Approvals.VerifyJson(content);
    }
    [Fact]
    public async Task can_dispatch_to_an_openai_pipeline()
    {
        var result = await _httpClient.PostAsync(
            "/openai/deployments/openaiendpoint/chat/completions?api-version=2023-05-15",
            new StringContent(JsonConvert.SerializeObject(new
            {
            }), Encoding.UTF8, "application/json"));

        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();
        Approvals.VerifyJson(content);
    }

    [Fact]
    public async Task works_with_the_azure_sdk_chat_completions()
    {
        var client = new OpenAIClient(_httpClient.BaseAddress, new AzureKeyCredential("ignore"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var completions = await client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                Messages = { new ChatMessage(ChatRole.System, "Hello world!") },
                DeploymentName = "openaiendpoint"
            });
        
        completions.Value.Id.ShouldBe(AICentralFakeResponses.FakeResponseId);
    }
}