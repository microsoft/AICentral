using System.Net;
using System.Net.Http.Json;
using AICentral.ConsumerAuth.AICentralJWT;
using AICentralTests.TestHelpers;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.InBuiltJwtAuth;

public class the_inbuild_auth_provider : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_inbuild_auth_provider(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task can_create_and_use_jwt_tokens()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://not-used-for-tokens/aicentraljwt/azure-openai-to-azure-with_custom_jwt/tokens")
        {
            Content = JsonContent.Create(new
            {
                Names = new[] { "Hacker1", "Hacker2", "Hacker3", "Hacker4" },
                ValidPipelines = new[] { "azure-openai-to-azure-with_custom_jwt.localtest.me-pipeline" },
                ValidFor = "00:08:00"
            })
        };
        request.Headers.Add("api-key", "fake-admin-key");

        var tokensResponse = await _httpClient.SendAsync(request);
        tokensResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokens = (await tokensResponse.Content.ReadFromJsonAsync<AICentralJwtProviderResponse>())!;
        tokens.Tokens.Length.ShouldBe(4);

        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "random",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure-with_custom_jwt.localtest.me"),
            new AzureKeyCredential("ignore"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        (await Should.ThrowAsync<RequestFailedException>(() =>
                client.GetChatCompletionsAsync(new ChatCompletionsOptions("random",
                    [new ChatRequestSystemMessage("Test")])))
            ).Status.ShouldBe(401);

        client = new OpenAIClient(
            new Uri("http://azure-openai-to-azure-with_custom_jwt.localtest.me"),
            new AzureKeyCredential(tokens.Tokens[0].ApiKeyToken),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var response = await client.GetChatCompletionsAsync(new ChatCompletionsOptions("random",
                [new ChatRequestSystemMessage("Test")]));

        response.GetRawResponse().Status.ShouldBe(200);

    }
}