using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Newtonsoft.Json;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class when_performing_embeddings: IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public when_performing_embeddings(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task we_support_base_64_encoded_format()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/adatest/embeddings?api-version=2024-02-15-preview",
            OpenAIFakeResponses.FakeEmbeddingResponse);

        var result = await _httpClient.PostAsync(
            "http://azure-openai-to-azure.localtest.me/openai/deployments/adatest/embeddings?api-version=2024-02-15-preview",
            new StringContent(JsonConvert.SerializeObject(new
                {
                    input = new[]
                    {
                        1199
                    },
                    model = "text-embedding-ada-002",
                    encoding_format = "base64"
                }
            ), Encoding.UTF8, "application/json"));

        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        await Verify(_factory.Services.VerifyRequestsAndResponses(result, validateResponseMetadata:true));
    }

    public void Dispose()
    {
        _factory.Dispose();
        _httpClient.Dispose();
    }
}