using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentral;

public class IncomingCallDetector
{
    private readonly IEnumerable<IAIServiceDetector> _detectors;

    public IncomingCallDetector(IEnumerable<IAIServiceDetector> detectors)
    {
        _detectors = detectors;
    }

    public async Task<AICallInformation> Detect(HttpRequest request, CancellationToken cancellationToken)
    {
        using var
            requestReader = new StreamReader(request.Body);

        var requestRawContent = await requestReader.ReadToEndAsync(cancellationToken);

        var deserializedRequestContent =
            request.ContentType?.Contains("application/json", StringComparison.InvariantCultureIgnoreCase) ?? false
                ? JsonConvert.DeserializeObject(requestRawContent) as JObject
                : null;

        var aiService = _detectors.FirstOrDefault(x => x.CanDetect(request))
            ?.Detect(request, deserializedRequestContent);
        if (aiService == null)
        {
            throw new NotSupportedException("Cannot detect incoming request");
        }

        return new AICallInformation(
            aiService,
            deserializedRequestContent,
            QueryHelpers.ParseQuery(request.QueryString.Value ?? string.Empty)
        );
    }
}