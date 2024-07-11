namespace AICentral.Logging.PIIStripping;

public class PIIStrippingLoggerConfig
{
    public required string? StorageUri { get; init; }
    public required string? StorageQueueConnectionString { get; init; }
    public required string QueueName { get; init; }

    public required string? CosmosAccountEndpoint { get; init; }
    public required string CosmosDatabase { get; init; }
    public required string CosmosContainer { get; init; }

    public required string TextAnalyticsEndpoint { get; init; }
    public string? TextAnalyticsKey { get; init; }

    public bool UseManagedIdentities { get; init; }

}
