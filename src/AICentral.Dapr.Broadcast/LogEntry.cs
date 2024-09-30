namespace AICentral.Dapr.Broadcast;

internal record LogEntry(
    // ReSharper disable once InconsistentNaming
    string id,
    string? InternalEndpointName,
    string? OpenAIHost,
    string? ModelName,
    string? DeploymentName,
    string Client,
    string CallType,
    bool? StreamingResponse,
    string? Prompt,
    string? Response,
    int? EstimatedPromptTokens, 
    int? EstimatedCompletionTokens,
    int? PromptTokens, 
    int? CompletionTokens, 
    int? TotalTokens,
    string RemoteIpAddress,
    DateTimeOffset StartDate,
    TimeSpan Duration,
    bool? Success
)
{
}