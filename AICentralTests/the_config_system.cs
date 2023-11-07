using AICentral;
using AICentral.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace AICentralTests;

public class the_config_system
{
    public AICentralPipelineAssembler Build(Dictionary<string, string?> configuration)
    {
        return new ConfigurationBasedPipelineBuilder()
            .BuildPipelinesFromConfig(
                NullLogger.Instance,
                new ConfigurationBuilder()
                    .AddInMemoryCollection(configuration)
                    .Build()
                    .GetSection("AICentral")
            );
    }

    [Fact]
    public void does_not_break_with_empty_config()
    {
        Should.NotThrow(() => Build(new Dictionary<string, string?>()
            {
                ["AICentral"] = ""
            })
        );
    }

    [Fact]
    public void produces_helpful_errors_with_invalid_api_key_auth_no_clients()
    {
        Should.Throw<ArgumentException>(() =>
            Build(new Dictionary<string, string?>
            {
                { "AICentral:AuthProviders:0:Name", "ApiKeyTest" },
                { "AICentral:AuthProviders:0:Type", "ApiKey" },
            }));
    }

}