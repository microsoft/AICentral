namespace AICentral;

public record AICentralUsageInformation(string OpenAiHost, string Prompt, int EstimatedPromptTokens, int EstimatedCompletionTokens, int PromptTokens, int CompletionTokens, int TotalTokens, string RemoteIpAddress, DateTimeOffset StartDate, TimeSpan Duration);
public record AICentralRequestInformation(string LanguageUrl, string Prompt, DateTimeOffset StartDate, TimeSpan Duration);
