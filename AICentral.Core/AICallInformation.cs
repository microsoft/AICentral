using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace AICentral.Core;

public record IncomingCallDetails(AIServiceType ServiceType, AICallType AICallType, string? PromptText, string? IncomingModelName, JObject? RequestContent);

public record AICallInformation(
    IncomingCallDetails IncomingCallDetails,
    Dictionary<string, StringValues> QueryString);
