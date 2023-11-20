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
            "AzureOpenAI call. ClientIP:{ClientIP} Host:{OpenAiHost}. Type:{CallType}. Model:{Model}. Prompt:{Prompt}. Response:{Response}. Estimated Prompt Tokens:{EstimatedPromptTokens}. Estimated Completion Tokens:{EstimatedCompletionTokens}. Prompt Tokens:{PromptTokens}. Completion Tokens:{CompletionTokens}. Total Tokens:{TotalTokens}. Duration:{Duration}",
            result.AiCentralUsageInformation.RemoteIpAddress,
            result.AiCentralUsageInformation.OpenAiHost,
            result.AiCentralUsageInformation.CallType.ToString(),
            result.AiCentralUsageInformation.ModelName,
            _logPrompt ? result.AiCentralUsageInformation.Prompt : "**redacted**",
            _logResponse ? result.AiCentralUsageInformation.Response : "**redacted**",
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
            LogPrompt = _logPrompt,
            WorkspaceId = _workspaceId
        };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

    public static string ConfigName => "AzureMonitorLogger";
}