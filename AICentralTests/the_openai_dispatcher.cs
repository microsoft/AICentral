using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using ApprovalTests;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests;

public class the_openai_dispatcher : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public the_openai_dispatcher(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }
    
    [Fact]
    public async Task cannot_proxy_an_image_request_from_azure_openai_endpoint_to_openai_downstream()
    {
        //DALLE-2 is no longer reachable with the latest SDK!
        var response = await _httpClient.PostAsync(
            new Uri(
                "http://azure-openai-to-openai.localtest.me/openai/images/generations:submit?api-version=2023-09-01-preview"),
            new StringContent("", Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task can_dispatch_chat_completions_to_an_openai_pipeline()
    {
        _factory.Seed("https://api.openai.com/v1/chat/completions",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));
        
        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_05_15)
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var response = await client.GetChatCompletionsAsync(new ChatCompletionsOptions("openaiendpoint", new[]
        {
            new ChatRequestAssistantMessage("")
        }));

        Approvals.VerifyJson(response.Value.Choices[0].Message.Content);
    }
    
    [Fact]
    public async Task will_forward_dalle3_requests_to_openai()
    {
        _factory.Seed($"https://api.openai.com/v1/images/generations",
            () => Task.FromResult(AICentralFakeResponses.FakeOpenAIDALLE3ImageResponse()));


        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_12_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        var result = await client.GetImageGenerationsAsync(
            new ImageGenerationOptions()
            {
                Prompt = "Me building an Open AI Reverse Proxy",
                DeploymentName = "openaimodel"
            });
        
        result.Value.Data.Count.ShouldBe(1);
    }
    
}