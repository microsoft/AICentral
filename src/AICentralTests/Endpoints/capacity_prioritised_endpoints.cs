using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class capacity_prioritised_endpoints : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public capacity_prioritised_endpoints(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Theory]
    [InlineData(1, 5000, 1000, 10, 10, 50, 0)]
    [InlineData(3, 1000, 5000, 10, 10, 0, 50)]
    [InlineData(5, 1000, 1000, 100, 10, 50, 0)]
    [InlineData(7, 1000, 1000, 10, 100, 0, 50)]
    public async Task will_favour_those_with_higher_remaining_tokens_and_requests_available(
        int minutesToRollForward,
        int endpoint1RemainingTokens, 
        int endpoint2RemainingTokens, 
        int endpoint1RemainingRequests, 
        int endpoint2RemainingRequests,
        int expectedEndpoint1Requests, 
        int expectedEndpoint2Requests
        )
    {
        _factory.Services.GetRequiredService<FakeDateTimeProvider>().Advance(TimeSpan.FromMinutes(minutesToRollForward));
        
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse(remainingTokens:endpoint1RemainingTokens, remainingRequests: endpoint1RemainingRequests)));
        
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200Number2, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse(remainingTokens:endpoint2RemainingTokens, remainingRequests: endpoint2RemainingRequests)));

        var sendMessage = async () =>
        {
            await Task.Delay(Random.Shared.Next(0, 1000));
            return await _httpClient.PostAsync(
                $"http://azure-to-azure-openai-capacity-based.localtest.me/openai/deployments/Model1/chat/completions?api-version={OpenAITestEx.OpenAIClientApiVersion}",
                new StringContent(JsonConvert.SerializeObject(new
                {
                    messages = new[]
                    {
                        new { role = "user", content = "Do other Azure AI services support this too?" }
                    },
                    max_tokens = 5
                }), Encoding.UTF8, "application/json"));
        };
        
        //seed the 2 endpoints - then we'll get the capacity requirements loaded
        await sendMessage();
        await sendMessage();

        var responses = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => sendMessage()));
        var servers = responses.Select(x => x.Headers.GetValues("x-aicentral-server").Single()).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

        if (expectedEndpoint1Requests > 0)
        {
            servers[TestPipelines.Endpoint200].ShouldBe(expectedEndpoint1Requests);
        }

        if (expectedEndpoint2Requests > 0)
        {
            servers[TestPipelines.Endpoint200Number2].ShouldBe(expectedEndpoint2Requests);
        }

    }

    public void Dispose()
    {
    }
}