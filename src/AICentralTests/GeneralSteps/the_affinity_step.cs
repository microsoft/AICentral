using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.GeneralSteps;

public class the_affinity_step : IClassFixture<TestWebApplicationFactory<Program>>

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_affinity_step(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact(Skip = "Waiting for Azure SDK to support assistants. I don't need affinity for any other types")]
    public async Task can_add_affinity_to_requests_to_allow_stateful_services_like_assistants()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "random",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse(10)));
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200Number2, "random",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse(20)));

        _httpClient.DefaultRequestHeaders.Add("x-aicentral-affinity-key", "asdfasdfasd");
        
        var client = new OpenAIClient(
            new Uri("http://azure-to-azure-openai-random-with-affinity.localtest.me"),
            new AzureKeyCredential("123"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_12_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient),
            });


        //the affinity is set upon the first request being successful. After that, the chosen endpoint is used for all requests
        var response1 =
            await client.GetChatCompletionsAsync(new ChatCompletionsOptions("random",
                [new ChatRequestSystemMessage("Test")]));

        var theRest = await Task.WhenAll(Enumerable.Range(1, 100).Select(_ =>
            client.GetChatCompletionsAsync(new ChatCompletionsOptions("random",
                [new ChatRequestSystemMessage("Test")]))));

        var chosenServer = response1.GetRawResponse().Headers.Single(x => x.Name == "x-aicentral-server").Value!;

        var responseServers = theRest
            .Select(x => x.GetRawResponse().Headers.Single(h => h.Name == "x-aicentral-server").Value)
            .ToArray();
        
        responseServers
            .ShouldAllBe(x => x == chosenServer);
    }
}