using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Auth.ApiKey;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.OpenAI;
using AICentral.PipelineComponents.EndpointSelectors.Priority;
using AICentral.PipelineComponents.EndpointSelectors.Random;
using AICentral.PipelineComponents.Routes;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace AICentralTests;

public class the_config_system
{
    [Fact]
    public void produces_helpful_errors_with_invalid_routes()
    {
        Should.Throw<ArgumentException>(() => PathMatchRouter.BuildFromConfig(""));
    }

    [Fact]
    public void produces_helpful_errors_with_invalid_endpoints()
    {
        Should.Throw<ArgumentException>(() =>
            OpenAIEndpointDispatcherBuilder.BuildFromConfig(new ConfigurationBuilder().Build()
                .GetSection("AICentral")));
    }

    [Fact]
    public void produces_helpful_errors_with_invalid_random_endpoint_selector()
    {
        Should.Throw<ArgumentException>(() =>
            RandomEndpointSelectorBuilder.BuildFromConfig(new ConfigurationBuilder().Build().GetSection("AICentral"),
                new Dictionary<string, IAICentralEndpointDispatcherBuilder>()));
    }

    [Fact]
    public void produces_helpful_errors_with_invalid_prioritised_endpoint_selector()
    {
        Should.Throw<ArgumentException>(() =>
            PriorityEndpointSelectorBuilder.BuildFromConfig(new ConfigurationBuilder().Build().GetSection("AICentral"),
                new Dictionary<string, IAICentralEndpointDispatcherBuilder>()));
    }

    [Fact]
    public void produces_helpful_errors_with_invalid_api_key_auth()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "Test:Properties", string.Empty }
        }!);

        Should.Throw<ArgumentException>(() => ApiKeyClientAuthBuilder.BuildFromConfig(config.Build().GetSection("Test"))
        );
    }

    [Fact]
    public void produces_helpful_errors_with_invalid_api_key_auth_no_header()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "Test:Properties:Clients:0:ClientName", string.Empty }
        }!);

        Should.Throw<ArgumentException>(() => ApiKeyClientAuthBuilder.BuildFromConfig(config.Build().GetSection("Test"))
        );
    }

    [Fact]
    public void produces_helpful_errors_with_invalid_api_key_auth_empty_clients()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "Test:Properties:Clients:0:ClientName", string.Empty }
        }!);

        Should.Throw<ArgumentException>(() => ApiKeyClientAuthBuilder.BuildFromConfig(config.Build().GetSection("Test"))
        );
    }
}