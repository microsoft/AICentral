namespace AICentral.Logging.PIIStripping;

public class PIIStrippingLoggerConfig
{
    public required string StorageQueueConnectionString { get; init; }
    public required string QueueName { get; init; }

    public required string CosmosConnectionString { get; init; }
    public required string CosmosDatabase { get; init; }
    public required string CosmosContainer { get; init; }

    public required string TextAnalyticsEndpoint { get; init; }
    public required string TextAnalyticsKey { get; init; }

}
