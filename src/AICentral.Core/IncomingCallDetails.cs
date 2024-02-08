using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

<<<<<<< HEAD
public record IncomingCallDetails(string PipelineName, AICallType AICallType, string? PromptText, string? IncomingModelName, JsonDocument? RequestContent, Dictionary<string, StringValues>? QueryString, string? PreferredEndpoint);
=======
public record IncomingCallDetails(string PipelineName, AICallType AICallType, string? PromptText, string? IncomingModelName, string? IncomingAssistantName, JsonDocument? RequestContent, Dictionary<string, StringValues>? QueryString);
>>>>>>> de73077 (Added affinity bits into this)
