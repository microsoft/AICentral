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
    
    public string? TextAnalyticsKey { get; init; }

    public required string QueueName { get; init; }
    public required string CosmosDatabase { get; init; }
    public required string CosmosContainer { get; init; }
    public required string TextAnalyticsEndpoint { get; init; }

    public bool UseManagedIdentities { get; init; }
    public string? UserAssignedManagedIdentityId { get; init; }

    /// <summary>
    /// Set to true to avoid PII Stripping (i.e. log raw prompts and responses).
    /// </summary>
    public bool PIIStrippingDisabled { get; init; } = false;
}