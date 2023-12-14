using System.Reflection;
using AICentral.Core;
using AICentral.OpenAI.AzureOpenAI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AICentral.Configuration;

public static class ConfigurationEx
{
    public static IServiceCollection AddAICentral(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "AICentral",
        ILogger? startupLogger = null,
        Action<AICentralConfig>? configureOptions = null,
        params Assembly[] additionalComponentAssemblies)
    {
        var logger = startupLogger ?? NullLogger.Instance;
        logger.LogInformation("AICentral - Initialising pipelines");

        var configFromSection = configuration.GetSection(configSectionName);
        var typedConfig = configFromSection.Exists()
            ? configFromSection.Get<AICentralConfig>()!
            : new AICentralConfig();
        typedConfig.FillInPropertiesFromConfiguration(configuration.GetSection(configSectionName));
        configureOptions?.Invoke(typedConfig);

        var configurationPipelineBuilder = new ConfigurationBasedPipelineBuilder()
            .BuildPipelinesFromConfig(
                typedConfig,
                logger,
                additionalComponentAssemblies.Concat(new[]
                        { typeof(AzureOpenAIEndpointRequestResponseHandler).Assembly, typeof(AICentralPipelineAssembler).Assembly })
                    .ToArray());

        configurationPipelineBuilder.AddServices(services, typedConfig.HttpMessageHandler, logger);

        return services;
    }

    public static void UseAICentral(this WebApplication webApplication)
    {
        var aiCentral = webApplication.Services.GetRequiredService<ConfiguredPipelines>();
        aiCentral.BuildRoutes(webApplication);
    }
}