using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace AICentral;

public record AICallInformation(
    IAIServiceDetector IncomingCallDetails,
    JObject? RequestContent,
    Dictionary<string, StringValues> QueryString);

public interface IAIServiceDetector
{
    AIServiceType ServiceType { get; }
    AICallType AICallType { get; }
    string? PromptText { get; }
    string? IncomingModelName { get; }
}

public enum AIServiceType
{
    OpenAI,
    AzureOpenAI
}