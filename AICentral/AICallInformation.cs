using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace AICentral;

public record AICallInformation(
    AICallType AICallType, 
    string? IncomingModelName, 
    JObject RequestContent,
    string PromptText, 
    string RemainingUrl,
    Dictionary<string, StringValues> QueryString);