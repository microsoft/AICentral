using AICentral.Configuration.JSON;
using Microsoft.Extensions.Logging.Abstractions;

namespace AICentral.Configuration;

public static class ConfigurationEx
{
    public static IServiceCollection AddAICentral(
        this IServiceCollection services,
        AICentralOptions providedOptions,
        ILogger? startupLogger = null)
    {
        var aiCentral = new AICentral(providedOptions);
        aiCentral.AddServices(services);
        services.AddSingleton(aiCentral);
        return services;
    }

    public static IServiceCollection AddAICentral(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "AICentral",
        ILogger? startupLogger = null)
    {
        var logger = startupLogger ?? NullLogger.Instance;
        logger.LogInformation("AICentral - Initialising pipelines");

        var configSection = configuration.GetSection(configSectionName);
        var optionsFromConfig = new ConfigurationBasedPipelineBuilder().BuildPipelinesFromConfig(logger,
            configuration.GetSection(configSectionName),
            configSection.Get<ConfigurationTypes.AICentralConfig>());
        services.AddAICentral(optionsFromConfig, startupLogger);

        return services;
    }

    public static WebApplication UseAICentral(
        this WebApplication webApplication)
    {
        var aiCentral = webApplication.Services.GetRequiredService<AICentral>();
        var logger = webApplication.Services.GetRequiredService<ILogger<AICentral>>();
        aiCentral.MapRoutes(webApplication, logger);
        return webApplication;
    }
}