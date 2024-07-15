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

public class content_filters : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public content_filters(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task can_handle_streaming_content_filters()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "ModelStream",
            OpenAIFakeResponses.FakeStreamingChatCompletionsResponseContentFilter);

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        var completions = await client.GetChatCompletionsStreamingAsync(
            new ChatCompletionsOptions("ModelStream", new[]
            {
                new ChatRequestSystemMessage("You are a helpful assistant.")
            }));

        var output = new StringBuilder();

        await foreach (var completion in completions)
        {
            output.Append(completion.ContentUpdate);
        }

        await Verify(_factory.Services.VerifyRequestsAndResponses(completions.GetRawResponse(), true));
    }

    [Fact]
    public async Task will_not_retry_if_content_filter_breached()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "Model",
            OpenAIFakeResponses.FakeContentFilterJailbreakAttempt);

        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200Number2, "Model",
            OpenAIFakeResponses.FakeContentFilterJailbreakAttempt);

        var client = new OpenAIClient(
            new Uri("http://azure-to-azure-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        var completions = await Should.ThrowAsync<RequestFailedException>(() => client.GetChatCompletionsAsync(
            new ChatCompletionsOptions("Model", new[]
            {
                new ChatRequestSystemMessage("You are a helpful assistant.")
            })));

        //This is a legit bad-request from the user. Not a failed server.
        completions.GetRawResponse()!.Headers.Contains("x-aicentral-failed-servers").ShouldBeFalse();

        await Verify(_factory.Services.VerifyRequestsAndResponses(completions.GetRawResponse()!, true));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}