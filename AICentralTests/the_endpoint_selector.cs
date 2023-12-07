using System.Text;
using AICentral;
using AICentral.Configuration;
using AICentral.Configuration.JSON;
using ApprovalTests;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;

namespace AICentralTests;

public class the_endpoint_selector
{
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
                            Properties = new ConfigurationTypes.AICentralPipelineAzureOpenAIEndpointPropertiesConfig()
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
                        new ConfigurationTypes.AICentralPipelineConfig()
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
                            Properties = new ConfigurationTypes.AICentralPipelineAzureOpenAIEndpointPropertiesConfig()
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
                        new ConfigurationTypes.AICentralPipelineConfig()
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
}