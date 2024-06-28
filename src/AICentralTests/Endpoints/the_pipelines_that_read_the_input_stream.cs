using AICentralOpenAIMock;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class the_pipelines_that_read_the_input_stream : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    public void Dispose()
    {
        _factory.Services.ClearSeededMessages();
    }

    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public the_pipelines_that_read_the_input_stream(TestWebApplicationFactory<Program> factory,
        ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task can_handle_failed_downstreams()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/whisper/audio/transcriptions?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeOpenAIAudioTranscriptionResponse()));

        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200Number2}/openai/deployments/whisper/audio/transcriptions?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.NotFoundResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-to-azure-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2024_02_15_Preview)
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        await using var stream =
            typeof(the_azure_openai_pipeline).Assembly.GetManifestResourceStream(
                "AICentralTests.Assets.Recording.m4a")!;

        var response = await client.GetAudioTranscriptionAsync(new AudioTranscriptionOptions()
        {
            Prompt = "I think it's something to do with programming",
            DeploymentName = "whisper",
            Temperature = 0.7f,
            ResponseFormat = AudioTranscriptionFormat.Vtt,
            AudioData = await BinaryData.FromStreamAsync(stream)
        });

        response.Value.ShouldNotBeNull();
        await Verify(_factory.Services.VerifyRequestsAndResponses(response));
    }
}