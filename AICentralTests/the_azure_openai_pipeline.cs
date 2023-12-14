using System.Net;
using System.Text;
using AICentralTests.TestHelpers;
using AICentralWeb;
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
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;

    public the_azure_openai_pipeline(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
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
    public async Task cannot_proxy_an_image_request_from_azure_openai_endpoint_to_openai_downstream()
    {
        //DALLE-2 is no longer reachable with the latest SDK!
        var response = await _httpClient.PostAsync(
            new Uri(
                "http://azure-openai-to-openai.localtest.me/openai/images/generations:submit?api-version=2023-09-01-preview"),
            new StringContent("", Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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

        //DALLE-2 is no longer reachable with the latest SDK!
        var result = await _httpClient.PostAsync(
            new Uri(
                "http://azure-openai-to-openai.localtest.me/openai/images/generations:submit?api-version=2023-05-15"),
            new StringContent("", Encoding.UTF8, "application/json"));
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
            typeof(the_azure_openai_pipeline).Assembly.GetManifestResourceStream(
                "AICentralTests.Assets.Recording.m4a")!;
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
    public async Task will_follow_affinity_requests_to_allow_async_against_a_multi_endpoint()
    {
        //test will fail on the first endpoint, so has to pick the second. This test should always pass, as the 2nd call to check the image completion status
        //will always have an affinity header to the working endpoint.
        _factory.Seed(
            $"https://{AICentralFakeResponses.Endpoint200}/openai/images/generations:submit?api-version=2023-09-01-preview",
            () => Task.FromResult(AICentralFakeResponses.NotFoundResponse()));

        _factory.Seed(
            $"https://{AICentralFakeResponses.Endpoint200}/openai/operations/images/f508bcf2-e651-4b4b-85a7-58ad77981ffa?api-version=2023-09-01-preview",
            () => Task.FromResult(AICentralFakeResponses.NotFoundResponse()));

        _factory.Seed(
            $"https://{AICentralFakeResponses.Endpoint200Number2}/openai/images/generations:submit?api-version=2023-09-01-preview",
            () => Task.FromResult(AICentralFakeResponses.FakeAzureOpenAIImageResponse()));

        _factory.Seed(
            $"https://{AICentralFakeResponses.Endpoint200Number2}/openai/operations/images/f508bcf2-e651-4b4b-85a7-58ad77981ffa?api-version=2023-09-01-preview",
            () => Task.FromResult(AICentralFakeResponses.FakeAzureOpenAIImageStatusResponse()));

        //DALLE-2 is no longer reachable with the latest SDK!
        var response = await _httpClient.PostAsync(
            new Uri(
                "http://azure-to-azure-openai.localtest.me/openai/images/generations:submit?api-version=2023-09-01-preview"),
            new StringContent("", Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task can_handle_streaming_calls()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "ModelStream",
            () => Task.FromResult(AICentralFakeResponses.FakeStreamingCompletionsResponse()), "2023-09-01-preview");

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2023_09_01_Preview)
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        var completions = await client.GetChatCompletionsStreamingAsync(
            new ChatCompletionsOptions("ModelStream", new[]
            {
                new ChatRequestSystemMessage("You are a helpful assistant.")
            }));

        var output = new StringBuilder();
        await foreach (var completion in completions)
        {
            output.Append(completion.ContentUpdate);
        }

        Approvals.Verify(output);
    }

    [Fact]
    public async Task can_model_map_dalle3_requests()
    {
        _factory.Seed(
            $"https://{AICentralFakeResponses.Endpoint200}/openai/deployments/Model1/images/generations?api-version=2023-12-01-preview",
            () => Task.FromResult(AICentralFakeResponses.FakeAzureOpenAIDALLE3ImageResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
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
                DeploymentName = "gpt-3.5-turbo"
            });

        result.Value.Data.Count.ShouldBe(1);
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
                DeploymentName = "openaimodel"
            });
        
        result.Value.Data.Count.ShouldBe(1);
    }
}