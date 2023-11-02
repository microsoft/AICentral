using AICentral.PipelineComponents.Endpoints;

namespace AICentral;

public record AICentralUsageInformation(string OpenAiHost, string ModelName, string Client, AICallType CallType, string Prompt, int EstimatedPromptTokens, int EstimatedCompletionTokens, int PromptTokens, int CompletionTokens, int TotalTokens, string RemoteIpAddress, DateTimeOffset StartDate, TimeSpan Duration);
public record AICentralRequestInformation(string LanguageUrl, AICallType CallType, string Prompt, DateTimeOffset StartDate, TimeSpan Duration);
