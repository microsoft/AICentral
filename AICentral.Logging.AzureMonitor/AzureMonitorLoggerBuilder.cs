using AICentral.Core;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AICentral.Logging.AzureMonitor;

/// <summary>
/// Logs out usage information to Azure Monitor
/// </summary>
public class AzureMonitorLoggerBuilder : IAICentralGenericStepBuilder<IAICentralPipelineStep>
{
    private readonly string _workspaceId;
    private readonly string _key;
    private readonly bool _logPrompt;
    private readonly bool _logResponse;

    public AzureMonitorLoggerBuilder(
        string workspaceId,
        string key,
        bool logPrompt,
        bool logResponse)
    {
        _workspaceId = workspaceId;
        _key = key;
        _logPrompt = logPrompt;
        _logResponse = logResponse;
    }

    public static string ConfigName => "AzureMonitorLogger";

    public static IAICentralGenericStepBuilder<IAICentralPipelineStep> BuildFromConfig(
        ILogger logger, 
        IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties")
            .Get<AzureMonitorLoggingConfig>()!;
        
        Guard.NotNull(properties, configurationSection, "Properties");

        return new AzureMonitorLoggerBuilder(
            Guard.NotNull(properties.WorkspaceId, configurationSection, nameof(properties.WorkspaceId)),
            Guard.NotNull(properties.Key, configurationSection, nameof(properties.Key)),
            Guard.NotNull(properties.LogPrompt, configurationSection, nameof(properties.LogPrompt))!.Value,
            Guard.NotNull(properties.LogResponse, configurationSection, nameof(properties.LogResponse))!.Value
        );
    }

    public IAICentralPipelineStep Build()
    {
        return new AzureMonitorLogger(new LoggerConfiguration().WriteTo.AzureAnalytics(
                _workspaceId,
                _key
            ).CreateLogger(), _workspaceId, _logPrompt, _logResponse
        );
    }

    public void RegisterServices(IServiceCollection services)
    {
    }
}