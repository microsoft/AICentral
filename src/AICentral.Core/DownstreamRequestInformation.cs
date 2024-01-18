namespace AICentral.Core;

public record DownstreamRequestInformation(string LanguageUrl, AICallType CallType, string? DeploymentName, string? Prompt, DateTimeOffset StartDate, TimeSpan Duration);
