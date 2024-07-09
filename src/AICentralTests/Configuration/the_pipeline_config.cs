using System.Text;
using AICentral;
using AICentral.Configuration;
using AICentral.Core;
using AICentral.Endpoints.AzureOpenAI;
using AICentral.Endpoints.AzureOpenAI.Authorisers.BearerPassThroughWithAdditionalKey;
using AICentral.Endpoints.OpenAI;
using AICentral.Logging.AzureMonitor.AzureMonitorLogging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace AICentralTests.Configuration;

public class the_pipeline_config
{
    [Fact]
    public Task supports_minimal_setup()
    {
        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                AICentral = new
                {
                    Endpoints = new object[]
                    {
                        new
                        {
                            Type = "AzureOpenAIEndpoint",
                            Name = "test-endpoint",
                            Properties = new AzureOpenAIEndpointPropertiesConfig()
                            {
                                AuthenticationType = "BearerPlusKeyName",
                                LanguageEndpoint = "https://somehere.com",
                            }
                        },
                        new
                        {
                            Type = "OpenAIEndpoint",
                            Name = "test-endpoint-1",
                            Properties = new OpenAIEndpointPropertiesConfig()
                            {
                                ApiKey = "fake-key",
                                ModelMappings = new Dictionary<string, string>()
                                {
                                    ["Test"] = "TestMap"
                                }
                            }
                        }
                    },
                    BackendAuths = new[]
                    {
                        new
                        {
                            Type = "BearerPlusKey",
                            Name = "BearerPlusKeyName",
                            Properties = new
                            {
                                IncomingClaimName = "test",
                                KeyHeaderName = "api-key",
                                ClaimsToKeys = new ClaimValueToSubscriptionKey[]
                                {
                                    new ()
                                    {
                                        ClaimValue = "User1",
                                        SubscriptionKey = "Key1"
                                    },
                                    new ()
                                    {
                                        ClaimValue = "User2",
                                        SubscriptionKey = "Key2"
                                    },
                                }
                            }
                        }
                    },
                    EndpointSelectors = new[]
                    {
                        new
                        {
                            Type = "SingleEndpoint",
                            Name = "default-endpoint-selector",
                            Properties = new
                            {
                                Endpoint = "test-endpoint"
                            }
                        }
                    },
                    AuthProviders = new object[]
                    {
                        new
                        {
                            Type = "AllowAnonymous",
                            Name = "anonymous"
                        },
                        new
                        {
                            Type = "Entra",
                            Name = "azure-open-ai-token-checker",
                            Properties = new
                            {
                                Entra = new
                                {
                                    ClientId = "https://cognitiveservices.azure.com",
                                    TenantId = "16b3c013-d300-468d-ac64-7eda0820b6d3",
                                    Instance = "https://login.microsoftonline.com/",
                                    Audience = "https://cognitiveservices.azure.com"
                                }
                            }
                        }
                    },
                    Pipelines = new[]
                    {
                        new PipelineConfig()
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

        var host = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = "test" });
        host.Configuration.AddJsonStream(stream);
        host.Services.AddAICentral(host.Configuration,
            additionalComponentAssemblies: typeof(AzureMonitorLogger).Assembly);
        var app = host.Build();

        var pipelines = app.Services.GetRequiredService<ConfiguredPipelines>();
        return Verify(pipelines.WriteDebug());
    }
}