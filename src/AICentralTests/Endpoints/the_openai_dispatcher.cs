using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class the_openai_dispatcher : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public the_openai_dispatcher(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task can_proxy_a_azure_dall_e_2_request_to_openai()
    {
        _factory.Seed("https://api.openai.com/v1/images/generations",
            () => Task.FromResult(AICentralFakeResponses.FakeOpenAIDALLE3ImageResponse()));

        //DALLE-2 is no longer reachable with the latest SDK!
        var response = await _httpClient.PostAsync(
            new Uri(
                "http://azure-openai-to-openai.localtest.me/openai/images/generations:submit?api-version=2023-12-01-preview"),
            new StringContent(JsonConvert.SerializeObject(new { prompt = "draw me something blue" }), Encoding.UTF8,
                "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await Verify(_factory.VerifyRequestsAndResponses(response));
    }

    [Fact]
    public async Task can_dispatch_chat_completions_to_an_openai_pipeline()
    {
        _factory.Seed("https://api.openai.com/v1/chat/completions",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_12_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var response = await client.GetChatCompletionsAsync(new ChatCompletionsOptions("openaimodel", new[]
        {
            new ChatRequestAssistantMessage("")
        }));

        await Verify(_factory.VerifyRequestsAndResponses(response.Value));
    }

    [Fact]
    public async Task will_forward_whisper_transcription_requests_to_openai()
    {
        _factory.Seed($"https://api.openai.com/v1/audio/transcriptions",
            () => Task.FromResult(AICentralFakeResponses.FakeOpenAIAudioTranscriptionResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_12_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        await using var stream =
            typeof(the_azure_openai_pipeline).Assembly.GetManifestResourceStream(
                "AICentralTests.Assets.Recording.m4a")!;
        var result = await client.GetAudioTranscriptionAsync(
            new AudioTranscriptionOptions
            {
                ResponseFormat = AudioTranscriptionFormat.Simple,
                DeploymentName = "test",
                AudioData = await BinaryData.FromStreamAsync(stream)
            });

        await Verify(_factory.VerifyRequestsAndResponses(result));
    }

    [Fact]
    public async Task will_forward_whisper_transcription_requests_to_openai_with_a_failed_endpoints()
    {
        _factory.Seed($"https://api.openai.com/v1/audio/transcriptions",
            req => req.Headers.Authorization?.Parameter == "ignore-fake-key-4323431"
                ? Task.FromResult(AICentralFakeResponses.FakeOpenAIAudioTranscriptionResponse())
                : Task.FromResult(AICentralFakeResponses.NotFoundResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-multiple-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_12_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        await using var stream =
            typeof(the_azure_openai_pipeline).Assembly.GetManifestResourceStream(
                "AICentralTests.Assets.Recording.m4a")!;
        var result = await client.GetAudioTranscriptionAsync(
            new AudioTranscriptionOptions
            {
                ResponseFormat = AudioTranscriptionFormat.Simple,
                DeploymentName = "openaimodel",
                AudioData = await BinaryData.FromStreamAsync(stream)
            });

        await Verify(_factory.VerifyRequestsAndResponses(result));
    }

    [Fact]
    public async Task will_forward_whisper_translation_requests_to_openai()
    {
        _factory.Seed($"https://api.openai.com/v1/audio/translations",
            () => Task.FromResult(AICentralFakeResponses.FakeOpenAIAudioTranslationResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_12_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        await using var stream =
            typeof(the_azure_openai_pipeline).Assembly.GetManifestResourceStream(
                "AICentralTests.Assets.Recording.m4a")!;
        var result = await client.GetAudioTranslationAsync(
            new AudioTranslationOptions()
            {
                ResponseFormat = AudioTranslationFormat.Simple,
                DeploymentName = "random",
                AudioData = await BinaryData.FromStreamAsync(stream)
            });

        result.Value.Text.ShouldNotBe(null);
    }


    [Fact]
    public async Task will_forward_dalle3_requests_to_openai()
    {
        _factory.Seed($"https://api.openai.com/v1/images/generations",
            () => Task.FromResult(AICentralFakeResponses.FakeOpenAIDALLE3ImageResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_12_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        var result = await client.GetImageGenerationsAsync(
            new ImageGenerationOptions()
            {
                Prompt = "Me building an Open AI Reverse Proxy",
                DeploymentName = "random"
            });

        await Verify(_factory.VerifyRequestsAndResponses(result));
    }
    
    [Fact]
    public async Task can_handle_streaming_calls()
    {
        _factory.Seed("https://api.openai.com/v1/chat/completions", AICentralFakeResponses.FakeOpenAIStreamingCompletionsResponse);

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_12_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        var completions = await client.GetChatCompletionsStreamingAsync(
            new ChatCompletionsOptions("openaimodel", new[]
            {
                new ChatRequestSystemMessage("You are a helpful assistant.")
            }));

        var output = new StringBuilder();
        await foreach (var completion in completions)
        {
            output.Append(completion.ContentUpdate);
        }

        await Verify(_factory.VerifyRequestsAndResponses(output));
    }

    [Fact]
    public async Task will_return_a_second_endpoint_when_no_model_mapping_on_the_first()
    {
        _factory.Seed("https://api.openai.com/v1/chat/completions", () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));
        
 
        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-multiple-openai-different-model-mappings.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_12_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var response = await client.GetChatCompletionsAsync(new ChatCompletionsOptions("openaimodel1", new[]
        {
            new ChatRequestAssistantMessage("")
        }));

        await Verify(_factory.VerifyRequestsAndResponses(response.Value));
        
    }

    public void Dispose()
    {
        _factory.Clear();
    }
}