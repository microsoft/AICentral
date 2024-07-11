namespace AICentral.Logging.PIIStripping;

public class PIIStrippingLoggerConfig
{
    /// <summary>
    /// Used for Managed Identity
    /// </summary>
    public string? CosmosAccountEndpoint { get; init; }
    /// <summary>
    /// Used for Managed Identity
    /// </summary>
    public string? StorageUri { get; init; }

    /// <summary>
    /// Used when no Managed Identity
    /// </summary>
    public string? StorageQueueConnectionString { get; init; }
    /// <summary>
    /// Used when no Managed Identity
    /// </summary>
    public string? CosmosConnectionString { get; init; }
    /// <summary>
    /// Used when no Managed Identity
    /// </summary>
    public string? TextAnalyticsKey { get; init; }

    public required string QueueName { get; init; }
    public required string CosmosDatabase { get; init; }
    public required string CosmosContainer { get; init; }
    public required string TextAnalyticsEndpoint { get; init; }

    public bool UseManagedIdentities { get; init; }

}
