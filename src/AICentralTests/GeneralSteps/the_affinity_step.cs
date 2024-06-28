using AICentralOpenAIMock;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using Azure.Core.Pipeline;
using OpenAIMock;
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

    [Fact]
    public async Task can_add_affinity_to_requests_to_allow_stateful_services_like_assistants()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/assistants/ass-assistant-123-out?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeAzureOpenAIAssistantResponse("ass-assistant-123-out")));

        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200Number2}/openai/assistants/ass-assistant-123-out?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeAzureOpenAIAssistantResponse("ass-assistant-123-out")));

        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/threads/thread-1/messages?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeMessageResponse("thread-123")));

        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200Number2}/openai/threads/thread-1/messages?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeMessageResponse("thread-123")));

        _httpClient.DefaultRequestHeaders.Add("x-aicentral-affinity-key", "asdfasdfasd");
        
        var client = new AssistantsClient(
            new Uri("http://azure-to-azure-openai-random-with-affinity.localtest.me"),
            new AzureKeyCredential("ignore-fake-key-123"),
            new AssistantsClientOptions(version: AssistantsClientOptions.ServiceVersion.V2024_02_15_Preview)
            {
                Transport = new HttpClientTransport(_httpClient)
            });


        //the affinity is set upon the first request being successful. After that, the chosen endpoint is used for all requests
        var response1 = await client.GetAssistantAsync("assistant-in");

        //interact with a threads.
        var theRest = await Task.WhenAll(Enumerable.Range(1, 100).Select(_ =>
            client.CreateMessageAsync("thread-1", MessageRole.User, "test")));

        var chosenServer = response1.GetRawResponse().Headers.Single(x => x.Name == "x-aicentral-server").Value!;

        var responseServers = theRest
            .Select(x => x.GetRawResponse().Headers.Single(h => h.Name == "x-aicentral-server").Value)
            .ToArray();
        
        responseServers
            .ShouldAllBe(x => x == chosenServer);
    }
}