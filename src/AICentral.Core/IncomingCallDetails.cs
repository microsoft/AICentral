using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

public record IncomingCallDetails(AICallType AICallType, string? PromptText, string? IncomingModelName, JsonDocument? RequestContent, Dictionary<string, StringValues>? QueryString);
