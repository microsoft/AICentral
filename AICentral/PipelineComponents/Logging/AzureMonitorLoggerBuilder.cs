using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Endpoints.AzureOpenAI;
using Serilog;

namespace AICentral.PipelineComponents.Logging;

/// <summary>
/// Logs out usage information to Azure Monitor
/// </summary>
public class AzureMonitorLoggerBuilder : IAICentralGenericStepBuilder<IAICentralPipelineStep>
{
    private readonly string _workspaceId;
    private readonly string _key;
    private readonly bool _logPrompt;

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

    public static IAICentralGenericStepBuilder<IAICentralPipelineStep> BuildFromConfig(
        IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.AzureMonitorLoggingConfig>()!;
        Guard.NotNull(properties, configurationSection, "Properties");

        return new AzureMonitorLoggerBuilder(
            Guard.NotNull(properties.WorkspaceId, configurationSection, nameof(properties.WorkspaceId)),
            Guard.NotNull(properties.Key, configurationSection, nameof(properties.Key)),
            Guard.NotNull(properties.LogPrompt, configurationSection, nameof(properties.LogPrompt))!.Value);
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