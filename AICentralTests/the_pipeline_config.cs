using System.Text;
using AICentral;
using AICentral.Configuration;
using AICentral.Configuration.JSON;
using ApprovalTests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
                            Properties = new ConfigurationTypes.AICentralPipelineAzureOpenAIEndpointPropertiesConfig()
                            {
                                ApiKey = "1234",
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
                    Pipelines = new[]
                    {
                        new ConfigurationTypes.AICentralPipelineConfig()
                        {
                            Name = "test-pipeline",
                            Host = "my-test-host.localtest.me",
                            EndpointSelector = "default-endpoint-selector",
                        }
                    }
                }
            }))
        );

        var host = WebApplication.CreateBuilder();
        host.Configuration.AddJsonStream(stream);
        host.Services.AddAICentral(host.Configuration);
        var app = host.Build();

        var pipelines = app.Services.GetRequiredService<AICentralPipelines>();
        Approvals.VerifyJson(JsonConvert.SerializeObject(pipelines.WriteDebug(), Formatting.Indented));
    }
}