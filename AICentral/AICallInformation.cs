using Newtonsoft.Json.Linq;

namespace AICentral.PipelineComponents.Endpoints;

public record AICallInformation(
    AICallType AICallType, 
    string IncomingModelName, 
    JObject RequestContent,
    string PromptText, 
    string RemainingUrl);