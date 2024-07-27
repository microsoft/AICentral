using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

public record IncomingCallDetails(string PipelineName, AICallType AICallType, AICallResponseType AICallResponseType, string? PromptText, string? IncomingModelName, string? IncomingAssistantName, JsonNode? RequestContent, string? PreferredEndpoint);
