using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AICentral.Pipelines.EndpointSelectors.Random;
using Newtonsoft.Json;
using Shouldly;

namespace AICentralTests;

public class the_random_endpoint_picker : IClassFixture<TestWebApplicationFactory<Program>>

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_random_endpoint_picker(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task works_with_a_single_endpoint()
    {
        var result = await _httpClient.PostAsync("/openai/deployments/random/chat/completions?api-version=2023-05-15",
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "Does Azure OpenAI support customer managed keys?" },
                    new { role = "assistant", content = "Yes, customer managed keys are supported by Azure OpenAI." },
                    new { role = "user", content = "Do other Azure AI services support this too?" }
                },
                max_tokens = 5
            }), Encoding.UTF8, "application/json"));
        
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}