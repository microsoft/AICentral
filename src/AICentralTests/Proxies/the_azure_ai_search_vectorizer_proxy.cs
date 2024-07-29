using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Newtonsoft.Json;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Proxies;

public class the_azure_ai_search_vectorizer_proxy : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public the_azure_ai_search_vectorizer_proxy(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
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

    
    [Fact]
    public async Task handles_downstream_failures()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/embeddings/embeddings?api-version=2024-04-01-preview",
            () => Task.FromResult(OpenAIFakeResponses.NotFoundResponse()));

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
        
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    
    [Fact]
    public async Task blocks_image_embedding_requests()
    {
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
                            imageBinary = new
                            {
                                data = Convert.ToBase64String(Encoding.UTF8.GetBytes("ignore"))
                            }
                        }
                    }
                }
            }))
        };

        var result = await _httpClient.SendAsync(request);
        
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task blocks_image_embedding_requests_using_urls()
    {
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
                            imageUrl = "https://someimage.localtest.me/image"
                        }
                    }
                }
            }))
        };

        var result = await _httpClient.SendAsync(request);
        
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}