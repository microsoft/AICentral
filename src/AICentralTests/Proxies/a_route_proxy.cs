using System.Net;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Newtonsoft.Json;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Proxies;

public class a_route_proxy : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public a_route_proxy(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }
    
    [Fact]
    public async Task enables_input_requests_to_be_remapped()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/embeddings/embeddings?api-version=2024-04-01-preview",
            OpenAIFakeResponses.FakeEmbeddingResponse);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://azure-with-aisearch-route-proxy.localtest.me/proxypath")
        {
            Content = new StringContent(JsonConvert.SerializeObject(new
            {
                values = new[]
                {
                    new
                    {
                        recordId = 0,
                        data = new
                        {
                            text = "this is a test"
                        }
                    }
                }
            }))
        };

        var result = await _httpClient.SendAsync(request);
        
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        await Verify(_factory.Services.VerifyRequestsAndResponses(result, true));
    }

    public void Dispose()
    {
        _factory.Dispose();
        _httpClient.Dispose();
    }
}