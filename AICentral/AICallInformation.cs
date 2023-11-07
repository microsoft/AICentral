using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace AICentral;

public record AICallInformation(
    AIServiceType AIServiceType,
    AICallType AICallType, 
    string? IncomingModelName, 
    JObject RequestContent,
    string PromptText, 
    Dictionary<string, StringValues> QueryString);