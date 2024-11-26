using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure.AI.Inference;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class inference_endpoints: IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public inference_endpoints(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task can_call_an_inference_endpoint()
    {
        _factory.Services.SeedInferenceChatCompletions(TestPipelines.Endpoint200, OpenAIFakeResponses.FakeInferenceChatCompletionsResponse);
        
        var client = new Azure.AI.Inference.ChatCompletionsClient(
            new Uri("http://azure-openai-to-azure.localtest.me/models"),
            new Azure.AzureKeyCredential("ignore"),
            new Azure.AI.Inference.AzureAIInferenceClientOptions()
            {
                Transport = new Azure.Core.Pipeline.HttpClientTransport(_httpClient),
            });

        var response = await client.CompleteAsync(new ChatCompletionsOptions(
            [new ChatRequestSystemMessage("hello")]));
        
        response.Value.ShouldNotBeNull();
        await Verify(_factory.Services.VerifyRequestsAndResponses(response, validateResponseMetadata:true));

    }
    
    [Fact]
    public async Task can_call_an_embeddings_endpoint()
    {
        _factory.Services.Seed($"https://{TestPipelines.Endpoint200}/models/embeddings?api-version=2024-05-01-preview", OpenAIFakeResponses.FakeInferenceEmbeddingResponse);
        
        var client = new Azure.AI.Inference.EmbeddingsClient(
            new Uri("http://azure-openai-to-azure.localtest.me/models"),
            new Azure.AzureKeyCredential("ignore"),
            new Azure.AI.Inference.AzureAIInferenceClientOptions()
            {
                Transport = new Azure.Core.Pipeline.HttpClientTransport(_httpClient),
            });

        var response = await client.EmbedAsync(new EmbeddingsOptions(
            ["hello"])
        {
            Model = "test"
        });
        
        response.Value.ShouldNotBeNull();
        await Verify(_factory.Services.VerifyRequestsAndResponses(response, validateResponseMetadata:true));

    }

    public void Dispose()
    {
        _factory.Services.ClearSeededMessages();
    }
}