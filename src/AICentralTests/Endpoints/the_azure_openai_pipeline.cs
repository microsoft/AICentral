using System.Net;
using System.Text;
using AICentralOpenAIMock;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class the_azure_openai_pipeline : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
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
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse()));
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200Number2, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse()));

        var result = await _httpClient.PostAsync(
            $"http://azure-to-azure-openai.localtest.me/openai/deployments/Model1/chat/completions?api-version={OpenAITestEx.OpenAIClientApiVersion}",
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

        await Verify(_factory.Services.VerifyRequestsAndResponses(result));

        result.Headers.GetValues("x-aicentral-pipeline").Single()
            .ShouldBe("azure-to-azure-openai.localtest.me-pipeline");
    }

    [Fact]
    public async Task handles_single_prompt_completions()
    {
        _factory.Services.SeedCompletions(TestPipelines.Endpoint200, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.FakeCompletionsResponse()));

        _factory.Services.SeedCompletions(TestPipelines.Endpoint200Number2, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.FakeCompletionsResponse()));

        var result = await _httpClient.PostAsync(
            $"http://azure-to-azure-openai.localtest.me/openai/deployments/Model1/completions?api-version={OpenAITestEx.OpenAIClientApiVersion}",
            new StringContent(JsonConvert.SerializeObject(new
            {
                prompt = "Hello world",
                max_tokens = 5
            }), Encoding.UTF8, "application/json"));

        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        await Verify(_factory.Services.VerifyRequestsAndResponses(result));
    }


    [Fact]
    public async Task works_with_the_azure_sdk_completions()
    {
        _factory.Services.SeedCompletions(TestPipelines.Endpoint200, "random",
            () => Task.FromResult(OpenAIFakeResponses.FakeCompletionsResponse()));
        _factory.Services.SeedCompletions(TestPipelines.Endpoint200Number2, "random",
            () => Task.FromResult(OpenAIFakeResponses.FakeCompletionsResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-to-azure-openai.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var completions = await client.GetCompletionsAsync(
            new CompletionsOptions()
            {
                Prompts = { "Hello world!" },
                DeploymentName = "random"
            });

        completions.Value.Id.ShouldBe(OpenAIFakeResponses.FakeResponseId);
    }

    [Fact]
    public async Task can_proxy_a_whisper_audio_request()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/whisper-1/audio/transcriptions?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeOpenAIAudioTranscriptionResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
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
            DeploymentName = "whisper-1",
            Temperature = 0.7f,
            ResponseFormat = AudioTranscriptionFormat.Vtt,
            AudioData = await BinaryData.FromStreamAsync(stream)
        });

        response.Value.ShouldNotBeNull();
        await Verify(_factory.Services.VerifyRequestsAndResponses(response));
    }

    [Fact]
    public async Task can_proxy_embedding_requests()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/adatest/embeddings?api-version=2024-02-15-preview",
            OpenAIFakeResponses.FakeEmbeddingResponse);

        var result = await _httpClient.PostAsync(
            "http://azure-openai-to-azure.localtest.me/openai/deployments/adatest/embeddings?api-version=2024-02-15-preview",
            new StringContent(JsonConvert.SerializeObject(new
            {
                input = "Test"
            }), Encoding.UTF8, "application/json"));

        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        await Verify(_factory.Services.VerifyRequestsAndResponses(result, validateResponseMetadata: true));
    }


    [Fact]
    public async Task can_proxy_embedding_requests_array()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/adatest/embeddings?api-version={OpenAITestEx.OpenAIClientApiVersion}",
            OpenAIFakeResponses.FakeEmbeddingArrayResponse);

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var response = await client.GetEmbeddingsAsync(new EmbeddingsOptions()
        {
            DeploymentName = "adatest",
            Input = { "Test1", "Test2" }
        });

        response.Value.ShouldNotBeNull();
        await Verify(_factory.Services.VerifyRequestsAndResponses(response, validateResponseMetadata: true));
    }

    [Fact]
    public async Task will_follow_affinity_requests_to_allow_async_against_a_multi_endpoint()
    {
        //test will fail on the first endpoint, so has to pick the second. This test should always pass, as the 2nd call to check the image completion status
        //will always have an affinity header to the working endpoint.
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/images/generations:submit?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.NotFoundResponse()));

        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/operations/images/f508bcf2-e651-4b4b-85a7-58ad77981ffa?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.NotFoundResponse()));

        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200Number2}/openai/images/generations:submit?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeAzureOpenAIImageResponse(TestPipelines.Endpoint200Number2)));

        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200Number2}/openai/operations/images/f508bcf2-e651-4b4b-85a7-58ad77981ffa?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeAzureOpenAIImageStatusResponse()));

        //DALLE-2 is no longer reachable with the latest SDK!
        var response = await _httpClient.PostAsync(
            new Uri(
                "http://azure-to-azure-openai.localtest.me/openai/images/generations:submit?api-version=2024-02-15-preview"),
            new StringContent(JsonConvert.SerializeObject(new
                { prompt = "Draw me an image" }), Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        response = await _httpClient.GetAsync(response.Headers.GetValues("operation-location").Single());

        await Verify(_factory.Services.VerifyRequestsAndResponses(response));
    }

    [Fact]
    public async Task can_handle_streaming_calls()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "ModelStream",
            OpenAIFakeResponses.FakeStreamingChatCompletionsResponse, "2024-02-15-preview");

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2024_02_15_Preview)
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

        await Verify(_factory.Services.VerifyRequestsAndResponses(output));
    }


    [Fact]
    public async Task can_handle_streaming_completions_calls()
    {
        _factory.Services.SeedCompletions(TestPipelines.Endpoint200, "ModelStream",
            OpenAIFakeResponses.FakeStreamingCompletionsResponse);

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        var completions = await client.GetCompletionsStreamingAsync(
            new CompletionsOptions("ModelStream", ["You are a helpful assistant."]));

        var output = new StringBuilder();

        await foreach (var completion in completions)
        {
            output.Append(completion.Choices.Any() ? completion.Choices[0].Text : string.Empty);
        }

        await Verify(_factory.Services.VerifyRequestsAndResponses(output));
    }

    [Fact]
    public async Task can_proxy_dalle2_requests()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/images/generations:submit?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeAzureOpenAIImageResponse(TestPipelines.Endpoint200)));

        var response = await _httpClient.PostAsync(
            new Uri(
                "http://azure-openai-to-azure.localtest.me/openai/images/generations:submit?api-version=2024-02-15-preview"),
            new StringContent(JsonConvert.SerializeObject(new { prompt = "draw me an image" }), Encoding.UTF8,
                "application/json"));

        await Verify(_factory.Services.VerifyRequestsAndResponses(response));
    }

    [Fact]
    public async Task can_proxy_dalle3_requests()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/gpt-3.5-turbo/images/generations?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeAzureOpenAIDALLE3ImageResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2024_02_15_Preview)
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

        await Verify(_factory.Services.VerifyRequestsAndResponses(result));
    }

    [Fact]
    public async Task handles_404s()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint404, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.NotFoundResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-404.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        await Should.ThrowAsync<RequestFailedException>(async () =>
            await client.GetChatCompletionsStreamingAsync(
                new ChatCompletionsOptions("Model1", new[]
                {
                    new ChatRequestSystemMessage("You are a helpful assistant.")
                })));
    }

    [Fact]
    public async Task handles_500_model_errors()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "Model1",
            () => Task.FromResult(OpenAIFakeResponses.FakeModelErrorResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            // ReSharper disable once RedundantArgumentDefaultValue
            new OpenAIClientOptions
            {
                Transport = new HttpClientTransport(_httpClient),
            });

        await Should.ThrowAsync<RequestFailedException>(async () =>
            await client.GetChatCompletionsAsync(
                new ChatCompletionsOptions("Model1", new[]
                {
                    new ChatRequestSystemMessage("You are going to chuck an odd model error.")
                })));
    }

    [Fact]
    public async Task works_with_the_azure_sdk_chat_completions()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "random",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var result = await client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestUserMessage(
                    [
                        new ChatMessageTextContentItem("I am some text"),
                        new ChatMessageImageContentItem(new Uri("http://image.localtest.me/1234")),
                        new ChatMessageImageContentItem(new Uri("http://image.localtest.me/1234"),
                            ChatMessageImageDetailLevel.High),
                        new ChatMessageImageContentItem(
                            new ChatMessageImageUrl(new Uri("http://image.localtest.me/1234"))),
                        new ChatMessageTextContentItem("And so am I!"),
                    ]),
                    new ChatRequestFunctionMessage("Function Message", "I am function output"),
                    new ChatRequestAssistantMessage("Assistant Message"),
                    new ChatRequestSystemMessage("System content")
                },
                DeploymentName = "random"
            });

        await Verify(_factory.Services.VerifyRequestsAndResponses(result.GetRawResponse(), true));
    }

    [Fact]
    public async Task can_map_models()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "mapped",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure-with-mapped-models.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var result = await client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestUserMessage(
                    [
                        new ChatMessageTextContentItem("I am some text"),
                    ]),
                },
                DeploymentName = "random"
            });

        result.GetRawResponse().Status.ShouldBe(200);
        await Verify(_factory.Services.VerifyRequestsAndResponses(result.GetRawResponse(), true));
    }


    [Fact]
    public async Task can_enforce_mapped_models()
    {
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "not-mapped",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure-with-mapped-models.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        (await Should.ThrowAsync<RequestFailedException>(() => client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatRequestUserMessage(
                    [
                        new ChatMessageTextContentItem("I am some text"),
                    ]),
                },
                DeploymentName = "not-mapped"
            }))).Status.ShouldBe(404);
    }

    [Fact]
    public async Task do_not_proxy_calls_to_the_base_path()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/",
            () => Task.FromResult(OpenAIFakeResponses.FakeModelErrorResponse()));

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://azure-openai-to-azure.localtest.me/");
        httpRequestMessage.Headers.Add("api-key", "ignore");
        var response = await _httpClient.SendAsync(httpRequestMessage);

        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task can_handle_token_completion_prompts()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/completionstest/completions?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeCompletionsResponse()));

        var result = await _httpClient.PostAsync(
            "http://azure-openai-to-azure.localtest.me/openai/deployments/completionstest/completions?api-version=2024-02-15-preview",
            new StringContent(JsonConvert.SerializeObject(new
            {
                prompt = (int[])
                [
                    200006,
                    17360,
                    200008
                ],
                max_tokens = 16384,
                temperature = 1,
                stream = false,
                format = "tokens",
                top_p = 1,
                stop = (int[][])
                [
                    [
                        200002
                    ],
                    [
                        200007
                    ]
                ],
                echo_stop = true,
                seed = 5327214743670833000,
                extensions =
                    "{\"json_object_after_dynamic_select\": {\"name_start_token\": 200003, \"name_end_tokens\": [200008], \"eot_tokens\": [200002, 200007]}}"
            }), Encoding.UTF8, "application/json"));
        
        await Verify(_factory.Services.VerifyRequestsAndResponses(result, validateResponseMetadata: true));

    }

    [Fact]
    public async Task can_handle_arrays_of_token_completion_prompts()
    {
        _factory.Services.Seed(
            $"https://{TestPipelines.Endpoint200}/openai/deployments/completionstest/completions?api-version=2024-02-15-preview",
            () => Task.FromResult(OpenAIFakeResponses.FakeCompletionsResponse()));

        var result = await _httpClient.PostAsync(
            "http://azure-openai-to-azure.localtest.me/openai/deployments/completionstest/completions?api-version=2024-02-15-preview",
            new StringContent(JsonConvert.SerializeObject(new
            {
                prompt = (int[][])
                [
                    [
                        200006,
                        17360,
                        200008
                    ],
                    [12, 32, 54423]
                ],
                max_tokens = 16384,
                temperature = 1,
                stream = false,
                format = "tokens",
                top_p = 1,
                stop = (int[][])
                [
                    [
                        200002
                    ],
                    [
                        200007
                    ]
                ],
                echo_stop = true,
                seed = 5327214743670833000,
                extensions =
                    "{\"json_object_after_dynamic_select\": {\"name_start_token\": 200003, \"name_end_tokens\": [200008], \"eot_tokens\": [200002, 200007]}}"
            }), Encoding.UTF8, "application/json"));
        
        await Verify(_factory.Services.VerifyRequestsAndResponses(result, validateResponseMetadata: true));

    }

    public void Dispose()
    {
        _factory.Services.ClearSeededMessages();
    }
}