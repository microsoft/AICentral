using System.ClientModel;
using System.ClientModel.Primitives;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure.AI.OpenAI;
using OpenAIMock;
using Xunit.Abstractions;

namespace AICentralTestsNewOpenAIClient;


public class works_with_embeddings : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public works_with_embeddings(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task can_use_new_sdk_with_embeddings()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/adatest/embeddings?api-version=2024-08-01-preview",
            OpenAIFakeResponses.FakeEmbeddingResponse);


        var client = new AzureOpenAIClient(
                new Uri("http://azure-to-azure-openai.localtest.me"),
                new ApiKeyCredential("ignore"),
                new AzureOpenAIClientOptions()
                {
                    Transport = new HttpClientPipelineTransport(_httpClient),
                    
                });

        var embeddings = await client
            .GetEmbeddingClient("adatest")
            .GenerateEmbeddingAsync("test");
    }

    public void Dispose()
    {
        _factory.Dispose();
        _httpClient.Dispose();
    }
}
