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
    public async Task can_use_new_sdk_with_base64_encoded_embeddings()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/text-embedding-ada-002/embeddings?api-version=2024-08-01-preview",
            OpenAIFakeResponses.FakeBase64EmbeddingResponse);

        var client = new AzureOpenAIClient(
                new Uri("http://azure-openai-to-azure.localtest.me"),
                new ApiKeyCredential("ignore"),
                new AzureOpenAIClientOptions()
                {
                    Transport = new HttpClientPipelineTransport(_httpClient)
                });

        var result = await client
            .GetEmbeddingClient("text-embedding-ada-002")
            .GenerateEmbeddingAsync("test");
        
        await Verify(_factory.Services.VerifyRequestsAndResponses(result.GetRawResponse(), true));
    }

    public void Dispose()
    {
        _factory.Dispose();
        _httpClient.Dispose();
    }
}
