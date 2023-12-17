using AICentral.Core;
using Microsoft.Extensions.Primitives;
using ILogger = Serilog.ILogger;

namespace AICentral.Logging.AzureMonitor.AzureMonitorLogging;

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
        _logPrompt = logPrompt;
        _logResponse = logResponse;
    }
    
    public async Task<AICentralResponse> Handle(HttpContext context, IncomingCallDetails aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var result = await pipeline.Next(context, aiCallInformation, cancellationToken);

        _serilogAzureLogAnalyticsLogger.Information(
            "AzureOpenAI call. ClientIP:{ClientIP} Host:{OpenAIHost}. Type:{CallType}. Model:{Model}. Prompt:{Prompt}. Response:{Response}. Estimated Prompt Tokens:{EstimatedPromptTokens}. Estimated Completion Tokens:{EstimatedCompletionTokens}. Prompt Tokens:{PromptTokens}. Completion Tokens:{CompletionTokens}. Total Tokens:{TotalTokens}. Duration:{Duration}",
            result.DownstreamUsageInformation.RemoteIpAddress,
            result.DownstreamUsageInformation.OpenAIHost,
            result.DownstreamUsageInformation.CallType.ToString(),
            result.DownstreamUsageInformation.ModelName,
            _logPrompt ? result.DownstreamUsageInformation.Prompt : "**redacted**",
            _logResponse ? result.DownstreamUsageInformation.Response : "**redacted**",
            result.DownstreamUsageInformation.EstimatedPromptTokens,
            result.DownstreamUsageInformation.EstimatedCompletionTokens,
            result.DownstreamUsageInformation.PromptTokens,
            result.DownstreamUsageInformation.CompletionTokens,
            result.DownstreamUsageInformation.TotalTokens,
            result.DownstreamUsageInformation.Duration);

        return result;
    }

    public Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}