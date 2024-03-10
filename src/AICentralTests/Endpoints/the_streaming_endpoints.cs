using System.Net;
using System.Text;
using AICentral;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Argon;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class the_streaming_endpoints : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public the_streaming_endpoints(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
        _httpClient.DefaultRequestVersion = HttpVersion.Version20;
        _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
    }

    [Fact]
    public async Task report_tokens_as_trailer_for_chat_completions()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            AICentralFakeResponses.FakeStreamingChatCompletionsResponse);

        var result = await _httpClient.PostAsync(
            "http://azure-openai-to-azure.localtest.me/openai/deployments/Model1/chat/completions?api-version=2024-02-15-preview",
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                },
                max_tokens = 5,
                streaming = true
            }), Encoding.UTF8, "application/json"));

        var trailer = result.TrailingHeaders.TryGetValues(Pipeline.XAiCentralStreamingTokenHeader, out var count);
        trailer.ShouldBeTrue();
        count!.Single().ShouldBe("73");
        
    }


    [Fact]
    public async Task report_tokens_as_trailer_for_completions()
    {
        _factory.SeedCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            AICentralFakeResponses.FakeStreamingCompletionsResponse);

        var result = await _httpClient.PostAsync(
            "http://azure-openai-to-azure.localtest.me/openai/deployments/Model1/completions?api-version=2024-02-15-preview",
            new StringContent(JsonConvert.SerializeObject(new
            {
                prompt =new[] {"You are a helpful assistant."},
                streaming = true
            }), Encoding.UTF8, "application/json"));

        var trailer = result.TrailingHeaders.TryGetValues(Pipeline.XAiCentralStreamingTokenHeader, out var count);
        trailer.ShouldBeTrue();
        count!.Single().ShouldBe("350");
        
    }

    public void Dispose()
    {
        _factory.Clear();
    }
}