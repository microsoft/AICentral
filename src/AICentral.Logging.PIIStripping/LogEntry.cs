using AICentral.Core;

namespace AICentral.DistributedTokenLimits;

internal record LogEntry(
    // ReSharper disable once InconsistentNaming
    string id,
    string? InternalEndpointName,
    string? OpenAIHost,
    string? ModelName,
    string? DeploymentName,
    string Client,
    AICallType CallType,
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
);