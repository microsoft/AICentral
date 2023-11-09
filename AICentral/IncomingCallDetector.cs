using Microsoft.AspNetCore.WebUtilities;

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
        var aiService = _detectors.FirstOrDefault(x => x.CanDetect(request));
        if (aiService == null)
        {
            throw new NotSupportedException("Cannot detect incoming request");
        }

        return new AICallInformation(
            await aiService.Detect(request, cancellationToken),
            QueryHelpers.ParseQuery(request.QueryString.Value ?? string.Empty)
        );
    }
}