using System.Net.Http.Headers;
using System.Security.Cryptography;
using AICentralTests.TestHelpers;
using AICentralTests.TestHelpers.FakeIdp;
using AICentralWeb;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenAIMock;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests.Endpoints;

public class bearer_plus_key_auth : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _httpClient;
    private readonly RsaSecurityKey _metadataProviderKey;

    public bearer_plus_key_auth(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
        _metadataProviderKey = new RsaSecurityKey(RSA.Create());
    }

    [Fact]
    public async Task can_augment_the_downstream_request_with_a_key()
    {
        IdentityModelEventSource.ShowPII = true;
            
        _factory.Services.SeedChatCompletions(TestPipelines.Endpoint200, "model",
            () => Task.FromResult(OpenAIFakeResponses.FakeChatCompletionsResponse()));

        var builder = new JwtBuilder(FakeIdpMessageHandler.Key, FakeIdpMessageHandler.TenantId, "test-client");
        var token = builder
            .WithAudience("https://cognitiveservices.azure.com")
            .IssuedToUser("user1")
            .FromTenantId(FakeIdpMessageHandler.TenantId)
            .IssuedToAppId(FakeIdpMessageHandler.FakeAppId)
            .Build();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var client = new OpenAIClient(
            new Uri("http://bearer-plus-key.localtest.me"),
            new AzureKeyCredential("throw-away"),
            new OpenAIClientOptions()
            {
                Transport = new HttpClientTransport(_httpClient)
            });

        var result = await client.GetChatCompletionsAsync(
            new ChatCompletionsOptions()
            {
                Messages = { new ChatRequestSystemMessage("Hello") },
                DeploymentName = "model"
            });

        result.GetRawResponse().Status.ShouldBe(200);
        
        await Verify(_factory.Services.VerifyRequestsAndResponses(result.GetRawResponse(), true));

    }

    public void Dispose()
    {
        _factory.Dispose();
        _httpClient.Dispose();
    }
}