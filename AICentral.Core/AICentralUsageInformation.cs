namespace AICentral.Core;

public record AICentralUsageInformation(string OpenAiHost, string? ModelName, string Client, AICallType CallType, string? Prompt, string? Response, int? EstimatedPromptTokens, int? EstimatedCompletionTokens, int? PromptTokens, int? CompletionTokens, int? TotalTokens, string RemoteIpAddress, DateTimeOffset StartDate, TimeSpan Duration);
