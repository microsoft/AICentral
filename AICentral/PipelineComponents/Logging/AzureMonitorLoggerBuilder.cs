using Serilog;
using Serilog.Core;

namespace AICentral.PipelineComponents.Logging;

/// <summary>
/// Logs out usage information to Azure Monitor
/// </summary>
public class AzureMonitorLoggerBuilder : IAICentralGenericStepBuilder<IAICentralPipelineStep>
{
    private readonly string _workspaceId;
    private readonly string _key;
    private readonly bool _logPrompt;
    private readonly Logger _azureMonitorLogger;

    public AzureMonitorLoggerBuilder(
        string workspaceId,
        string key,
        bool logPrompt)
    {
        _workspaceId = workspaceId;
        _key = key;
        _logPrompt = logPrompt;
    }

    public static string ConfigName => "AzureMonitorLogger";

    public static IAICentralGenericStepBuilder<IAICentralPipelineStep> BuildFromConfig(Dictionary<string, string> parameters)
    {
        return new AzureMonitorLoggerBuilder(
            parameters["WorkspaceId"],
            parameters["Key"],
            bool.TryParse(parameters["LogPrompt"], out var result) ? result : false);
    }

    public IAICentralPipelineStep Build()
    {
        return new AzureMonitorLogger(new LoggerConfiguration().WriteTo.AzureAnalytics(
                _workspaceId,
                _key
            ).CreateLogger(), _workspaceId, _logPrompt
        );
    }

    public void RegisterServices(IServiceCollection services)
    {
    }
}