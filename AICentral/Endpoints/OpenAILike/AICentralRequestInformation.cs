using AICentral.Core;

namespace AICentral.Endpoints.OpenAILike;

public record AICentralRequestInformation(string LanguageUrl, AICallType CallType, string? Prompt, DateTimeOffset StartDate, TimeSpan Duration);
