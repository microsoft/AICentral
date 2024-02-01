using AICentral;
using AICentral.Configuration;
using AICentral.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace AICentralTests.Configuration;

public class the_config_system
{
    public AICentralPipelineAssembler Build(Dictionary<string, string?> configuration)
    {
        var configurationSection = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration)
            .Build()
            .GetSection("AICentral");

        var configFromSection = configurationSection.Get<AICentralConfig>()!;
        configFromSection.FillInPropertiesFromConfiguration(configurationSection);

        return new ConfigurationBasedPipelineBuilder()
            .BuildPipelinesFromConfig(
                configFromSection,
                NullLogger.Instance
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