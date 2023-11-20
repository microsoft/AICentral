namespace AICentral.Core;

public record AICentralRequestInformation(string LanguageUrl, AICallType CallType, string? Prompt, DateTimeOffset StartDate, TimeSpan Duration);