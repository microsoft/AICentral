using AICentral.Core;
using Azure;
using Azure.AI.TextAnalytics;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AICentral.Logging.PIIStripping;

public class PIIStrippingLoggerFactory : IPipelineStepFactory
{
    private readonly string _stepName;
    private readonly PIIStrippingLoggerConfig _config;
    private readonly string _id;

    public PIIStrippingLoggerFactory(string stepName, PIIStrippingLoggerConfig config)
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
        TokenCredential credential = string.IsNullOrWhiteSpace(_config.UserAssignedManagedIdentityId)
            ? new DefaultAzureCredential()
            : new ManagedIdentityCredential(_config.UserAssignedManagedIdentityId);
        
        services.AddKeyedSingleton<QueueClient>(_id,
            (_, _) =>
                _config.UseManagedIdentities
                    ? new QueueClient(new Uri($"{_config.StorageUri!}/{_config.QueueName}"), credential)
                    : new QueueClient(_config.StorageQueueConnectionString, _config.QueueName));

        if (!_config.PIIStrippingDisabled)
        {
            services.AddKeyedSingleton<TextAnalyticsClient>(_id,
                (_, _) => new TextAnalyticsClient(new Uri(_config.TextAnalyticsEndpoint),
                    new AzureKeyCredential(_config.TextAnalyticsKey!)));
        }

        services.AddKeyedSingleton<CosmosClient>(_id,
            (_, _) =>
                _config.UseManagedIdentities
                    ? new CosmosClient(_config.CosmosAccountEndpoint, credential)
                    : new CosmosClient(_config.CosmosConnectionString)
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
            () => sp.GetRequiredKeyedService<TextAnalyticsClient>(_id),
            sp.GetRequiredKeyedService<CosmosClient>(_id),
            _config,
            sp.GetRequiredService<ILogger<PIIStrippingLoggerQueueConsumer>>(),
            sp.GetRequiredService<IDateTimeProvider>()
        ));
    }

    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var typedConfig = config.TypedProperties<PIIStrippingLoggerConfig>();

        if (!typedConfig.PIIStrippingDisabled)
        {
            Guard.NotNull(typedConfig.TextAnalyticsEndpoint, nameof(typedConfig.TextAnalyticsEndpoint));
            Guard.NotNull(typedConfig.TextAnalyticsKey, nameof(typedConfig.TextAnalyticsKey)); //RBAC not possible for language service
        }
        
        Guard.NotNull(typedConfig.QueueName, nameof(typedConfig.QueueName));

        if (typedConfig.UseManagedIdentities)
        {
            Guard.NotNull(typedConfig.StorageUri, nameof(typedConfig.StorageUri));
            Guard.NotNull(typedConfig.CosmosAccountEndpoint, nameof(typedConfig.CosmosAccountEndpoint));
            
            logger.LogInformation("PII Stripping logging will using Managed Identity {ClientId} to connect", typedConfig.UserAssignedManagedIdentityId);
            logger.LogInformation("Cosmos Endpoint {CosmosEndpoint}", typedConfig.CosmosAccountEndpoint);
            logger.LogInformation("Storage Endpoint {StorageEndpoint}", typedConfig.StorageUri);
        }
        else
        {
            Guard.NotNull(typedConfig.CosmosConnectionString, nameof(typedConfig.CosmosConnectionString));
            Guard.NotNull(typedConfig.StorageQueueConnectionString, nameof(typedConfig.StorageQueueConnectionString));
            logger.LogInformation("Logging will use Connection Strings to connect to Storage, Cosmos, and Language Service");

        }

        if (!typedConfig.PIIStrippingDisabled)
        {
            logger.LogInformation("Text Analytics Endpoint {TextEndpoint}", typedConfig.TextAnalyticsEndpoint);
        }
        else
        {
            logger.LogWarning("PII Stripping disabled. You will see raw prompts and responses");
        }

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