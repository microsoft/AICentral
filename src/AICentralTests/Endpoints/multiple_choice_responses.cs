using System.Net;
using System.Text;
using AICentralOpenAIMock;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Argon;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class multiple_choice_responses : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public multiple_choice_responses(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }
    
    [Fact]
    public async Task are_handled_correctly()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponseMultipleChoices()));

        var result = await _httpClient.PostAsync(
            $"http://azure-openai-to-azure.localtest.me/openai/deployments/Model1/chat/completions?api-version={OpenAITestEx.OpenAIClientApiVersion}",
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "user", content = "Do other Azure AI services support this too?" }
                },
                max_tokens = 5
            }), Encoding.UTF8, "application/json"));

        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        await Verify(_factory.Services.VerifyRequestsAndResponses(result, true));

        result.Headers.GetValues("x-aicentral-pipeline").Single().ShouldBe("azure-openai-to-azure.localtest.me-pipeline");
    }
    
     
    [Fact]
    public async Task are_handled_correctly_for_chats()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "ModelStream",
            OpenAIFakeResponses.FakeStreamingChatCompletionsResponseMultipleChoices, "2024-02-15-preview");

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2024_02_15_Preview)
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

        completions.GetRawResponse().Headers.TryGetValue("x-aicentral-test-diagnostics", out var key); 
        await Task.Yield(); //give asp.net time to finish up
        await Verify(_factory.Services.VerifyRequestsAndResponses(completions.GetRawResponse(), true));
    }
    
 
    public void Dispose()
    {
        _factory.Services.ClearSeededMessages();
    }
    
}