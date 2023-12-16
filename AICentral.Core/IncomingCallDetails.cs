using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace AICentral.Core;

public record IncomingCallDetails(AICallType AICallType, string? PromptText, string? IncomingModelName, JObject? RequestContent, Dictionary<string, StringValues>? QueryString);
