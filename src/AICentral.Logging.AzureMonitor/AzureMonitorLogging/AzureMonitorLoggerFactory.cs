using AICentral.Core;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AICentral.Logging.AzureMonitor.AzureMonitorLogging;

/// <summary>
/// Logs out usage information to Azure Monitor
/// </summary>
public class AzureMonitorLoggerFactory : IPipelineStepFactory
{
    private readonly string _workspaceId;
    private readonly bool _logPrompt;
    private readonly Lazy<AzureMonitorLogger> _logger;

    public AzureMonitorLoggerFactory(
        string workspaceId,
        string key,
        bool logPrompt,
        bool logResponse)
    {
        _workspaceId = workspaceId;
        _logPrompt = logPrompt;
        _logger = new Lazy<AzureMonitorLogger>(() => new AzureMonitorLogger(new LoggerConfiguration().WriteTo
                .AzureAnalytics(
                    _workspaceId,
                    key,
                    logName: "AILogs"
                ).CreateLogger(), _workspaceId, _logPrompt, logResponse
        ));
    }

    public static string ConfigName => "AzureMonitorLogger";

    public static IPipelineStepFactory BuildFromConfig(
        ILogger logger, 
        TypeAndNameConfig config)
    {
        var properties = config.TypedProperties<AzureMonitorLoggingConfig>()!;
        Guard.NotNull(properties, "Properties");

        return new AzureMonitorLoggerFactory(
            Guard.NotNull(properties.WorkspaceId, nameof(properties.WorkspaceId)),
            Guard.NotNull(properties.Key, nameof(properties.Key)),
            Guard.NotNull(properties.LogPrompt, nameof(properties.LogPrompt))!.Value,
            Guard.NotNull(properties.LogResponse, nameof(properties.LogResponse))!.Value
        );
    }

    public IPipelineStep Build(IServiceProvider serviceProvider)
    {
        return _logger.Value;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }
    
    public object WriteDebug()
    {
        return new
        {
            Type = "AzureMonitorLogging",
            LogPrompt = _logPrompt,
            WorkspaceId = _workspaceId
        };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }
}