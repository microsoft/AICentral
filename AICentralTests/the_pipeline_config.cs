using System.Text;
using AICentral;
using AICentral.Configuration;
using AICentral.Core;
using AICentral.Logging.AzureMonitor;
using AICentral.OpenAI.AzureOpenAI;
using ApprovalTests;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace AICentralTests;

public class the_pipeline_config
{
    [Fact]
    public void supports_minimal_setup()
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
                            Properties = new AzureOpenAIEndpointPropertiesConfig()
                            {
                                ApiKey = "1234",
                                AuthenticationType = "ApiKey",
                                LanguageEndpoint = "https://somehere.com",
                                ModelMappings = new Dictionary<string, string>()
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

        var host = WebApplication.CreateBuilder(new WebApplicationOptions() {EnvironmentName = "test"});
        host.Configuration.AddJsonStream(stream);
        host.Services.AddAICentral(host.Configuration,
            additionalComponentAssemblies: typeof(AzureMonitorLogger).Assembly);
        var app = host.Build();

        var pipelines = app.Services.GetRequiredService<ConfiguredPipelines>();
        Approvals.VerifyJson(JsonConvert.SerializeObject(pipelines.WriteDebug(), Formatting.Indented));
    }
}