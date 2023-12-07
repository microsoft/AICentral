using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using ApprovalTests;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests;

public class the_azure_openai_pipeline : IClassFixture<TestWebApplicationFactory<Program>>

{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_azure_openai_pipeline(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task handles_chats()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200Number2, "Model1",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var result = await _httpClient.PostAsync(
            "http://azure-to-azure-openai.localtest.me/openai/deployments/random/chat/completions?api-version=2023-05-15",
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
        var content = await result.Content.ReadAsStringAsync();
        Approvals.VerifyJson(content);
    }

    [Fact]
    public async Task can_dispatch_to_an_openai_pipeline()
    {
        _factory.Seed("https://api.openai.com/v1/chat/completions",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var result = await _httpClient.PostAsync(
            new Uri(
                "http://azure-openai-to-openai.localtest.me/openai/deployments/openaiendpoint/chat/completions?api-version=2023-05-15"),
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
        var content = await result.Content.ReadAsStringAsync();
        Approvals.VerifyJson(content);
    }

    [Fact]
    public async Task works_with_the_azure_sdk_completions()
    {
        _factory.SeedCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            () => Task.FromResult(AICentralFakeResponses.FakeCompletionsResponse()));
        _factory.SeedCompletions(AICentralFakeResponses.Endpoint200Number2, "Model1",
            () => Task.FromResult(AICentralFakeResponses.FakeCompletionsResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-to-azure-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_05_15)
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var completions = await client.GetCompletionsAsync(
            new CompletionsOptions()
            {
                Prompts = { "Hello world!" },
                DeploymentName = "random"
            });

        completions.Value.Id.ShouldBe(AICentralFakeResponses.FakeResponseId);
    }

    [Fact]
    public void cannot_proxy_an_image_request_from_azure_openai_endpoint_to_openai_downstream()
    {
        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_05_15)
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        Should.Throw<RequestFailedException>(async () =>
            await client.GetImageGenerationsAsync(
                new ImageGenerationOptions()
                {
                    Prompt = "Me building an Open AI Reverse Proxy"
                }));
    }

    [Fact]
    public async Task will_proxy_other_requests_to_a_single_endpoint()
    {
        _factory.Seed(
            $"https://{AICentralFakeResponses.Endpoint200}/openai/images/generations:submit?api-version=2023-09-01-preview",
            () => Task.FromResult(AICentralFakeResponses.FakeAzureOpenAIImageResponse()));

        _factory.Seed(
            $"https://{AICentralFakeResponses.Endpoint200}/openai/operations/images/f508bcf2-e651-4b4b-85a7-58ad77981ffa?api-version=2023-09-01-preview",
            () => Task.FromResult(AICentralFakeResponses.FakeAzureOpenAIImageStatusResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_09_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        var result = await client.GetImageGenerationsAsync(
            new ImageGenerationOptions()
            {
                Prompt = "Me building an Open AI Reverse Proxy",
            });
    }

    [Fact]
    public async Task can_proxy_a_whisper_audio_request()
    {
        _factory.Seed(
            $"https://{AICentralFakeResponses.Endpoint200}/openai/deployments/whisper-1/audio/transcriptions?api-version=2023-05-15",
            () => Task.FromResult(AICentralFakeResponses.FakeOpenAIAudioTranscriptionResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_05_15)
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        using var ms = new MemoryStream();
        await using var stream =
            typeof(the_openai_pipeline).Assembly.GetManifestResourceStream("AICentralTests.Assets.Recording.m4a")!;
        await stream.CopyToAsync(ms);

        var response = await client.GetAudioTranscriptionAsync(new AudioTranscriptionOptions()
        {
            Prompt = "I think it's something to do with programming",
            DeploymentName = "whisper-1",
            Temperature = 0.7f,
            ResponseFormat = AudioTranscriptionFormat.Vtt,
            AudioData = new BinaryData(ms.ToArray())
        });

        response.Value.ShouldNotBeNull();
    }

    [Fact]
    public void will_not_proxy_unknown_requests_to_a_multi_endpoint()
    {
        var client = new OpenAIClient(
            new Uri("http://azure-to-azure-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_09_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        Should.Throw<RequestFailedException>(async () =>
            await client.GetImageGenerationsAsync(
                new ImageGenerationOptions()
                {
                    Prompt = "Me building an Open AI Reverse Proxy",
                }));
    }
}