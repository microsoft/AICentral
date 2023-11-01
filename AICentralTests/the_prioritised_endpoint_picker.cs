using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests;

public class the_prioritised_endpoint_picker : IClassFixture<TestWebApplicationFactory<Program>>

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_prioritised_endpoint_picker(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task fails_over_to_a_successful_endpoint()
    {
        var result = await _httpClient.PostAsync("/openai/deployments/priority/chat/completions?api-version=2023-05-15",
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
        result.Headers.GetValues("x-aicentral-failed-servers").Single().ShouldBe($"https://{AICentralTestEndpointBuilder.Endpoint404}");
        result.Headers.GetValues("x-aicentral-server").Single().ShouldBe($"https://{AICentralTestEndpointBuilder.Endpoint200}");
    }
}