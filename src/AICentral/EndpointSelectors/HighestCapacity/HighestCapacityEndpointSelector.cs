using AICentral.Core;
using AICentral.Endpoints;
using AICentral.EndpointSelectors.Random;

namespace AICentral.EndpointSelectors.HighestCapacity;

public class HighestCapacitySelector : RandomEndpointSelector
{
    private readonly Dictionary<string,IEndpointDispatcher> _endpointDictionary;

    public HighestCapacitySelector(IEndpointDispatcher[] openAiServers) : base(openAiServers)
    {
        _endpointDictionary = openAiServers.ToDictionary(x => ((DownstreamEndpointDispatcher)x).HostName, x => x);
    }

    protected override IEnumerable<IEndpointDispatcher> NextEndpointEnumerator(IRequestContext context)
    {
        var rateLimitingTracker = context.RequestServices.GetRequiredService<DownstreamEndpointResponseDataTracker>();
        var now = context.RequestServices.GetRequiredService<IDateTimeProvider>().Now;
        var ordered = rateLimitingTracker.PrioritiseBasedOnMetrics(now, _endpointDictionary.Keys.ToArray());
        foreach (var endpoint in ordered)
        {
            yield return _endpointDictionary[endpoint];
        }
    }
} 