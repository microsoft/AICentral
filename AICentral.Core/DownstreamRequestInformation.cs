namespace AICentral.Core;

public record DownstreamRequestInformation(string LanguageUrl, AICallType CallType, string? Prompt, DateTimeOffset StartDate, TimeSpan Duration);
