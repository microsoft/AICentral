using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class sending_urls_to_openai : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public sending_urls_to_openai(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task ai_central_can_filter_them()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "model",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure-filter-chats.localtest.me"),
            new AzureKeyCredential("ignore-fake-key-123"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var result = await client.GetChatCompletionsAsync(
            new ChatCompletionsOptions("model",
            [
                new ChatRequestSystemMessage("You are an assistant"),
                new ChatRequestUserMessage(
                [
                    new ChatMessageTextContentItem("Normal text"),
                ]),
                new ChatRequestUserMessage(
                [
                    new ChatMessageImageContentItem(
                        new ChatMessageImageUrl(new Uri("http://somewherebad.com/myimage.jpeg"))),
                    new ChatMessageImageContentItem(
                        new Uri("http://somewheregood.com/myimage.jpeg")),
                    new ChatMessageImageContentItem(
                        new Uri($"data:image/jpeg;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes("Not a picture!"))}")),
                ])
            ])
        );

        result.GetRawResponse().Status.ShouldBe(200);

        await Verify(_factory.Services.VerifyRequestsAndResponses(result.GetRawResponse(), true));
    }

    public void Dispose()
    {
        _factory.Dispose();
        _httpClient.Dispose();
    }
}