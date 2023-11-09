using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral;

public class IncomingCallDetector
{
    public async Task<AICallInformation> Detect(HttpRequest request, CancellationToken cancellationToken)
    {
        using var
            requestReader = new StreamReader(request.Body);

        var requestRawContent = await requestReader.ReadToEndAsync(cancellationToken);
        var deserializedRequestContent = JsonConvert.DeserializeObject(requestRawContent) as JObject;

        var aiService = DetectAIService(request, deserializedRequestContent);

        return new AICallInformation(
            aiService,
            deserializedRequestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value ?? string.Empty)
        );
    }

    private static IAIServiceDetector DetectAIService(HttpRequest request, JObject? deserializedRequestContent)
    {
        //TODO maybe make this extensible
        if (request.Path.StartsWithSegments("/openai", out var deploymentPath))
        {
            return new AzureOpenAIServiceDetector(request, deserializedRequestContent, deploymentPath);
        }

        if (request.Path.StartsWithSegments("/v1", out var remainingPath))
        {
            return new OpenAIServiceDetector(deserializedRequestContent, remainingPath);
        }

        throw new NotSupportedException("Cannot detect incoming request");
    }
}