using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace AICentral;

public record IncomingCallDetails(AIServiceType ServiceType, AICallType AICallType, string? PromptText, string? IncomingModelName);

public record AICallInformation(
    IncomingCallDetails IncomingCallDetails,
    JObject? RequestContent,
    Dictionary<string, StringValues> QueryString);

public interface IAIServiceDetector
{
    bool CanDetect(HttpRequest request);
    IncomingCallDetails Detect(HttpRequest request, JObject? requestContent);
}


public enum AIServiceType
{
    OpenAI,
    AzureOpenAI
}