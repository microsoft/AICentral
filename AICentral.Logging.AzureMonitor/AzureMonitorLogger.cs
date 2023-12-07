using AICentral.Core;
using ILogger = Serilog.ILogger;

namespace AICentral.Logging.AzureMonitor;

public class AzureMonitorLogger : IAICentralPipelineStep
{
    private readonly ILogger _serilogAzureLogAnalyticsLogger;
    private readonly string _workspaceId;
    private readonly bool _logPrompt;
    private readonly bool _logResponse;

    public AzureMonitorLogger(ILogger serilogAzureLogAnalyticsLogger, string workspaceId, bool logPrompt, bool logResponse)
    {
        _serilogAzureLogAnalyticsLogger = serilogAzureLogAnalyticsLogger;
        _workspaceId = workspaceId;
        this._logPrompt = logPrompt;
        _logResponse = logResponse;
    }
    
    public async Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var result = await pipeline.Next(context, aiCallInformation, cancellationToken);

        _serilogAzureLogAnalyticsLogger.Information(
            "AzureOpenAI call. ClientIP:{ClientIP} Host:{OpenAIHost}. Type:{CallType}. Model:{Model}. Prompt:{Prompt}. Response:{Response}. Estimated Prompt Tokens:{EstimatedPromptTokens}. Estimated Completion Tokens:{EstimatedCompletionTokens}. Prompt Tokens:{PromptTokens}. Completion Tokens:{CompletionTokens}. Total Tokens:{TotalTokens}. Duration:{Duration}",
            result.AICentralUsageInformation.RemoteIpAddress,
            result.AICentralUsageInformation.OpenAIHost,
            result.AICentralUsageInformation.CallType.ToString(),
            result.AICentralUsageInformation.ModelName,
            _logPrompt ? result.AICentralUsageInformation.Prompt : "**redacted**",
            _logResponse ? result.AICentralUsageInformation.Response : "**redacted**",
            result.AICentralUsageInformation.EstimatedPromptTokens,
            result.AICentralUsageInformation.EstimatedCompletionTokens,
            result.AICentralUsageInformation.PromptTokens,
            result.AICentralUsageInformation.CompletionTokens,
            result.AICentralUsageInformation.TotalTokens,
            result.AICentralUsageInformation.Duration);

        return result;
    }
}