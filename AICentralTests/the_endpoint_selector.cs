using System.Net;
using System.Text;
using AICentral;
using AICentral.Configuration;
using AICentral.Steps.Endpoints.OpenAILike.AzureOpenAI;
using AICentralTests.TestHelpers;
using ApprovalTests;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace AICentralTests;

public class the_endpoint_selector : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public the_endpoint_selector(TestWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        factory.OutputHelper = testOutputHelper;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public void can_contain_an_endpoint_selector()
    {
        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                AICentral = new
                {
                    Endpoints = new[]
                    {
                        new
                        {
                            Type = "AzureOpenAIEndpoint",
                            Name = "test-endpoint",
                            Properties = new AICentralPipelineAzureOpenAIEndpointPropertiesConfig()
                            {
                                ApiKey = "1234",
                                LanguageEndpoint = "https://somehere.com",
                                ModelMappings = new Dictionary<string, string>(),
                                AuthenticationType = "ApiKey"
                            }
                        }
                    },
                    EndpointSelectors = new object[]
                    {
                        new
                        {
                            Type = "LowestLatency",
                            Name = "ll-selector",
                            Properties = new
                            {
                                Endpoints = new[] { "test-endpoint" }
                            }
                        },
                        new
                        {
                            Type = "SingleEndpoint",
                            Name = "default-endpoint-selector",
                            Properties = new
                            {
                                Endpoint = "ll-selector"
                            }
                        }
                    },
                    AuthProviders = new[]
                    {
                        new
                        {
                            Type = "AllowAnonymous",
                            Name = "anonymous"
                        }
                    },
                    Pipelines = new[]
                    {
                        new AICentralPipelineConfig()
                        {
                            Name = "test-pipeline",
                            Host = "my-test-host.localtest.me",
                            AuthProvider = "anonymous",
                            EndpointSelector = "default-endpoint-selector",
                        }
                    },
                }
            }))
        );

        var host = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = "tests" });
        host.Configuration.AddJsonStream(stream);
        host.Services.AddAICentral(host.Configuration);
        var app = host.Build();

        var pipelines = app.Services.GetRequiredService<AICentralPipelines>();
        var pipeline = JsonConvert.SerializeObject(pipelines.WriteDebug(), Formatting.Indented);
        Approvals.VerifyJson(pipeline);
    }

    [Fact]
    public void cannot_have_a_circular_reference()
    {
        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                AICentral = new
                {
                    Endpoints = new[]
                    {
                        new
                        {
                            Type = "AzureOpenAIEndpoint",
                            Name = "test-endpoint",
                            Properties = new AICentralPipelineAzureOpenAIEndpointPropertiesConfig()
                            {
                                ApiKey = "1234",
                                LanguageEndpoint = "https://somehere.com",
                                ModelMappings = new Dictionary<string, string>()
                            }
                        }
                    },
                    EndpointSelectors = new object[]
                    {
                        new
                        {
                            Type = "LowestLatency",
                            Name = "ll-selector",
                            Properties = new
                            {
                                Endpoints = new[] { "default-endpoint-selector" }
                            }
                        },
                        new
                        {
                            Type = "SingleEndpoint",
                            Name = "default-endpoint-selector",
                            Properties = new
                            {
                                Endpoint = "ll-selector"
                            }
                        }
                    },
                    AuthProviders = new[]
                    {
                        new
                        {
                            Type = "AllowAnonymous",
                            Name = "anonymous"
                        }
                    },
                    Pipelines = new[]
                    {
                        new AICentralPipelineConfig()
                        {
                            Name = "test-pipeline",
                            Host = "my-test-host.localtest.me",
                            AuthProvider = "anonymous",
                            EndpointSelector = "default-endpoint-selector",
                        }
                    },
                }
            }))
        );

        var host = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = "tests" });
        host.Configuration.AddJsonStream(stream);
        Should.Throw<ArgumentException>(() => host.Services.AddAICentral(host.Configuration));
    }

    [Fact]
    public async Task can_use_hierarchical_selectors()
    {
        _factory.SeedChatCompletions(AICentralFakeResponses.Endpoint200, "Model1",
            () => Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse()));

        var result = await _httpClient.PostAsync(
            "https://azure-hierarchical-selector.localtest.me/openai/deployments/random/chat/completions?api-version=2023-05-15",
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "user", content = "Do other Azure AI services support this too?" }
                },
                max_tokens = 5
            }), Encoding.UTF8, "application/json"));

        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}