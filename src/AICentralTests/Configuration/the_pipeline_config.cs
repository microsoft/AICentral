using System.Text;
using AICentral;
using AICentral.Configuration;
using AICentral.Core;
using AICentral.Endpoints.AzureOpenAI;
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
                    Endpoints = new object []
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
                    BackendAuthorisers = new []
                    {
                        new
                        {
                            Type="BearerPlusKey",
                            Name = "BearerPlusKeyName",
                            Properties = new
                            {
                                IncomingClaimName = "test",
                                KeyHeaderName = "api-key",
                                SubjectToKeyMappings = new
                                {
                                    User1 = "Key1",
                                    User2 = "Key2",
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

        var host = WebApplication.CreateBuilder(new WebApplicationOptions() {EnvironmentName = "test"});
        host.Configuration.AddJsonStream(stream);
        host.Services.AddAICentral(host.Configuration,
            additionalComponentAssemblies: typeof(AzureMonitorLogger).Assembly);
        var app = host.Build();

        var pipelines = app.Services.GetRequiredService<ConfiguredPipelines>();
        return Verify(pipelines.WriteDebug());
    }
}