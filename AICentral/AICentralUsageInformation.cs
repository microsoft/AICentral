namespace AICentral;

public record AICentralUsageInformation(string OpenAiHost, string Prompt, int EstimatedPromptTokens, int EstimatedCompletionTokens, int PromptTokens, int CompletionTokens, int TotalTokens, string RemoteIpAddress, DateTimeOffset StartDate, TimeSpan Duration);