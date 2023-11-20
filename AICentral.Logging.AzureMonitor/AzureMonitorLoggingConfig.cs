namespace AICentral.Logging.AzureMonitor;

public class AzureMonitorLoggingConfig
{
    public string? WorkspaceId { get; init; }
    public string? Key { get; init; }
    public bool? LogPrompt { get; init; }
    public bool? LogResponse { get; init; }
}