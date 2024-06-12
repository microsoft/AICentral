using AICentral.Core;
using Azure;
using Azure.AI.TextAnalytics;
using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AICentral.DistributedTokenLimits;

public class PIIStrippingLoggerFactory : IPipelineStepFactory
{
    private readonly string _stepName;
    private readonly PIIStrippingLoggerConfig _config;
    private readonly string _id;

    private PIIStrippingLoggerFactory(string stepName, PIIStrippingLoggerConfig config)
    {
        _stepName = stepName;
        _config = config;
        _id = Guid.NewGuid().ToString();
    }

    IPipelineStep IPipelineStepFactory.Build(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<PIIStrippingLogger>(_id);
    }

    public static string ConfigName => "PIIStrippingLogger";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKeyedSingleton<QueueClient>(_id,
            (_, _) => new QueueClient(
                _config.StorageQueueConnectionString,
                _config.QueueName));

        services.AddKeyedSingleton<TextAnalyticsClient>(_id,
            (_, _) => new TextAnalyticsClient(
                new Uri(_config.TextAnalyticsEndpoint),
                new AzureKeyCredential(_config.TextAnalyticsKey)));

        services.AddKeyedSingleton<CosmosClient>(_id,
            (_, _) => new CosmosClient(_config.CosmosConnectionString)
        );

        services.AddKeyedSingleton<PIIStrippingLogger>(
            _id,
            (sp, _) => new PIIStrippingLogger(
                _stepName,
                sp.GetRequiredKeyedService<QueueClient>(_id),
                _config,
                sp.GetRequiredService<ILogger<PIIStrippingLogger>>()
            ));

        services.AddHostedService<PIIStrippingLoggerQueueConsumer>(sp => new PIIStrippingLoggerQueueConsumer(
            sp.GetRequiredKeyedService<QueueClient>(_id),
            sp.GetRequiredKeyedService<TextAnalyticsClient>(_id),
            sp.GetRequiredKeyedService<CosmosClient>(_id),
            _config,
            sp.GetRequiredService<ILogger<PIIStrippingLoggerQueueConsumer>>()
        ));
    }

    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var typedConfig = config.TypedProperties<PIIStrippingLoggerConfig>();
        Guard.NotNull(typedConfig.TextAnalyticsEndpoint, nameof(typedConfig.TextAnalyticsEndpoint));
        Guard.NotNull(typedConfig.TextAnalyticsKey, nameof(typedConfig.TextAnalyticsKey));
        Guard.NotNull(typedConfig.CosmosConnectionString, nameof(typedConfig.CosmosConnectionString));
        Guard.NotNull(typedConfig.StorageQueueConnectionString, nameof(typedConfig.StorageQueueConnectionString));
        Guard.NotNull(typedConfig.QueueName, nameof(typedConfig.QueueName));

        return new PIIStrippingLoggerFactory(
            config.Name!,
            typedConfig
        );
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "AdvancedLogging",
            Queue = _config.QueueName
        };
    }

    public void ConfigureRoute(WebApplication webApplication, IEndpointConventionBuilder route)
    {
    }
}