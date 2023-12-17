using AICentral.Core;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AICentral.Logging.AzureMonitor.AzureMonitorLogging;

/// <summary>
/// Logs out usage information to Azure Monitor
/// </summary>
public class AzureMonitorLoggerFactory : IAICentralGenericStepFactory
{
    private readonly string _workspaceId;
    private readonly string _key;
    private readonly bool _logPrompt;
    private readonly bool _logResponse;
    private readonly Lazy<IAICentralPipelineStep> _logger;

    public AzureMonitorLoggerFactory(
        string workspaceId,
        string key,
        bool logPrompt,
        bool logResponse)
    {
        _workspaceId = workspaceId;
        _key = key;
        _logPrompt = logPrompt;
        _logResponse = logResponse;
        _logger = new Lazy<IAICentralPipelineStep>(() => new AzureMonitorLogger(new LoggerConfiguration().WriteTo
                .AzureAnalytics(
                    _workspaceId,
                    _key,
                    logName: "AILogs"
                ).CreateLogger(), _workspaceId, _logPrompt, _logResponse
        ));
    }

    public static string ConfigName => "AzureMonitorLogger";

    public static IAICentralGenericStepFactory BuildFromConfig(
        ILogger logger, 
        AICentralTypeAndNameConfig config)
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

    public IAICentralPipelineStep Build()
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