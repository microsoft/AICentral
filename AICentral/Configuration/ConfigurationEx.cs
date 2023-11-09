using System.Reflection;
using AICentral.Steps;
using Microsoft.Extensions.Logging.Abstractions;

namespace AICentral.Configuration;

public static class ConfigurationEx
{
    public static IServiceCollection AddAICentral(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "AICentral",
        ILogger? startupLogger = null,
        params Assembly[] additionalComponentAssemblies)
    {
        var logger = startupLogger ?? NullLogger.Instance;
        logger.LogInformation("AICentral - Initialising pipelines");

        var configurationPipelineBuilder = new ConfigurationBasedPipelineBuilder()
            .BuildPipelinesFromConfig(
                logger,
                configuration.GetSection(configSectionName),
                additionalComponentAssemblies);

        configurationPipelineBuilder.AddServices(services, logger);

        return services;
    }
    
    public static IServiceCollection AddAICentralNew(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "AICentral",
        ILogger? startupLogger = null,
        params Assembly[] additionalComponentAssemblies)
    {
        var logger = startupLogger ?? NullLogger.Instance;
        logger.LogInformation("AICentral - Initialising pipelines");

        var genericSteps = AssemblyEx.GetTypesOfType<IAICentralPipelineStep>();
        

        var configurationPipelineBuilder = new ConfigurationBasedPipelineBuilder()
            .BuildPipelinesFromConfig(
                logger,
                configuration.GetSection(configSectionName),
                additionalComponentAssemblies);

        configurationPipelineBuilder.AddServices(services, logger);

        return services;
    }

    public static void UseAICentral(this WebApplication webApplication)
    {
        var aiCentral = webApplication.Services.GetRequiredService<AICentralPipelines>();
        aiCentral.BuildRoutes(webApplication);
    }
}