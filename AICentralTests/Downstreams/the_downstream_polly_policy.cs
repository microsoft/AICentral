using System.Net;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Downstreams;

public class the_downstream_polly_policy : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_downstream_polly_policy(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task will_not_retry_429_until_retry_after_has_passed()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            () => Task.FromResult(AICentralFakeResponses.RateLimitResponse(TimeSpan.FromSeconds(5))));

        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200Number2, "Model1",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var fakeDateTimeProvider = _factory.Services.GetRequiredService<FakeDateTimeProvider>();

        //trigger the rate-limit on the 2 fake servers. Might take a few goes to hit the right one.
        var hitBadServer = false;
        for (var i = 0; i < 10; i++)
        {
            var responseStart = await _httpClient.PostChatCompletions("azure-to-azure-openai");
            responseStart.StatusCode.ShouldBe(HttpStatusCode.OK); //should always succeed
            hitBadServer = responseStart.Headers.Contains("x-aicentral-failed-servers");
            if (hitBadServer) break;
        }

        hitBadServer.ShouldBe(true);

        //all responses will now ignore the 429 server as it is rate limited
        var responseWhenLimited = await _httpClient.PostChatCompletions("azure-to-azure-openai");
        responseWhenLimited.StatusCode.ShouldBe(HttpStatusCode.OK); //should always succeed
        responseWhenLimited.Headers.GetValues("x-aicentral-server").Single()
            .ShouldBe($"https://{AICentralFakeResponses.Endpoint200Number2}");
        responseWhenLimited.Headers.Contains("x-aicentral-failed-servers").ShouldBeFalse();

        //advance past the rate-limit
        fakeDateTimeProvider.Advance(TimeSpan.FromSeconds(6));

        for (var i = 0; i < 10; i++)
        {
            var response = await _httpClient.PostChatCompletions("azure-to-azure-openai");
            response.StatusCode.ShouldBe(HttpStatusCode.OK); //should always succeed
            hitBadServer = response.Headers.Contains("x-aicentral-failed-servers");
            if (hitBadServer)
            {
                //as expected. We hit the bad server
                return;
            }
        }

        Assert.Fail("Never saw failed 429 server again even though retry has expired");
    }
}