using AICentral.PipelineComponents.Endpoints;
using ILogger = Serilog.ILogger;

namespace AICentral.PipelineComponents.Logging;

public class AzureMonitorLogger : IAICentralPipelineStep
{
    private readonly ILogger _serilogAzureLogAnalyticsLogger;
    private readonly string _workspaceId;
    private readonly bool _logPrompt;

    public AzureMonitorLogger(ILogger serilogAzureLogAnalyticsLogger, string workspaceId, bool logPrompt)
    {
        _serilogAzureLogAnalyticsLogger = serilogAzureLogAnalyticsLogger;
        _workspaceId = workspaceId;
        this._logPrompt = logPrompt;
    }
    
    public async Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var result = await pipeline.Next(context, aiCallInformation, cancellationToken);

        _serilogAzureLogAnalyticsLogger.Information(
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
            LogPrompt = _logPrompt,
            WorkspaceId = _workspaceId
        };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }
}