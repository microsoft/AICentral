using AICentral.Core;
using Microsoft.Extensions.Primitives;
using ILogger = Serilog.ILogger;

namespace AICentral.Logging.AzureMonitor.AzureMonitorLogging;

public class AzureMonitorLogger : IPipelineStep
{
    private readonly ILogger _serilogAzureLogAnalyticsLogger;
    private readonly string _workspaceId;
    private readonly bool _logPrompt;
    private readonly bool _logResponse;
    private readonly bool _logClient;

    public AzureMonitorLogger(
        ILogger serilogAzureLogAnalyticsLogger,
        string workspaceId,
        bool logPrompt,
        bool logResponse,
        bool logClient)
    {
        _serilogAzureLogAnalyticsLogger = serilogAzureLogAnalyticsLogger;
        _workspaceId = workspaceId;
        _logPrompt = logPrompt;
        _logResponse = logResponse;
        _logClient = logClient;
    }

    public async Task<AICentralResponse> Handle(
        IRequestContext context,
        IncomingCallDetails aiCallInformation,
        NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        var result = await next(context, aiCallInformation, cancellationToken);

        _serilogAzureLogAnalyticsLogger.Information(
            "AzureOpenAI call. ClientIP:{ClientIP}. Client Name:{ClientName}. Host:{OpenAIHost}. Type:{CallType}. StreamedResponse:{Streamed}. Deployment:{Deployment} Model:{Model}. Prompt:{Prompt}. Response:{Response}. Estimated Prompt Tokens:{EstimatedPromptTokens}. Estimated Completion Tokens:{EstimatedCompletionTokens}. Prompt Tokens:{PromptTokens}. Completion Tokens:{CompletionTokens}. Total Tokens:{TotalTokens}. Duration:{Duration}",
            result.DownstreamUsageInformation.RemoteIpAddress,
            _logClient ? context.User.Identity?.Name ?? string.Empty : "**redacted**",
            result.DownstreamUsageInformation.OpenAIHost,
            result.DownstreamUsageInformation.CallType.ToString(),
            result.DownstreamUsageInformation.StreamingResponse.HasValue
                ? result.DownstreamUsageInformation.StreamingResponse
                : string.Empty,
            result.DownstreamUsageInformation.DeploymentName,
            result.DownstreamUsageInformation.ModelName,
            _logPrompt ? result.DownstreamUsageInformation.Prompt : "**redacted**",
            _logResponse ? result.DownstreamUsageInformation.Response : "**redacted**",
            result.DownstreamUsageInformation.EstimatedTokens?.Value.EstimatedPromptTokens,
            result.DownstreamUsageInformation.EstimatedTokens?.Value.EstimatedCompletionTokens,
            result.DownstreamUsageInformation.KnownTokens?.PromptTokens,
            result.DownstreamUsageInformation.KnownTokens?.CompletionTokens,
            result.DownstreamUsageInformation.TotalTokens,
            result.DownstreamUsageInformation.Duration);

        return result;
    }

    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}