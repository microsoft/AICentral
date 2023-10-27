using Serilog;
using Serilog.Core;

namespace AICentral.Pipelines.Logging;

/// <summary>
/// Logs out usage information to Azure Monitor
/// </summary>
public class AzureMonitorLoggerPipelineStep : IAICentralPipelineStep
{
    private readonly string _workspaceId;
    private readonly bool _logPrompt;
    private readonly Logger _azureMonitorLogger;

    public AzureMonitorLoggerPipelineStep(
        string workspaceId, 
        string key,
        bool logPrompt)
    {
        _workspaceId = workspaceId;
        _logPrompt = logPrompt;
        _azureMonitorLogger = new LoggerConfiguration().WriteTo.AzureAnalytics(
            workspaceId,
            key
        ).CreateLogger();
    }

    public static string ConfigName => "AzureMonitorLogger";

    public static IAICentralPipelineStep BuildFromConfig(Dictionary<string, string> parameters)
    {
        return new AzureMonitorLoggerPipelineStep(
            parameters["WorkspaceId"],
            parameters["Key"],
            bool.TryParse(parameters["LogPrompt"], out var result) ? result : false);
    }

    public async Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var result = await pipeline.Next(context, cancellationToken);

        _azureMonitorLogger.Information(
            "AzureOpenAI call. ClientIP:{ClientIP} Host:{OpenAiHost}. Prompt:{Prompt}. Estimated Prompt Tokens:{EstimatedPromptTokens}. Estimated Completion Tokens:{EstimatedCompletionTokens}. Prompt Tokens:{PromptTokens}. Completion Tokens:{CompletionTokens}. Total Tokens:{TotalTokens}. Duration:{Duration}",
            result.AiCentralUsageInformation.RemoteIpAddress,
            result.AiCentralUsageInformation.OpenAiHost,
            _logPrompt ? result.AiCentralUsageInformation.Prompt : "**redacted**",
            result.AiCentralUsageInformation.EstimatedPromptTokens,
            result.AiCentralUsageInformation.EstimatedCompletionTokens,
            result.AiCentralUsageInformation.PromptTokens,
            result.AiCentralUsageInformation.CompletionTokens,
            result.AiCentralUsageInformation.TotalTokens,
            result.AiCentralUsageInformation.Duration);

        return result;
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "AzureMonitorLogging",
            LogPrompt = _logPrompt ,
            WorkspaceId = _workspaceId
        };
    }
}