using AICentral.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace AICentral.Affinity;

public class SingleNodeAffinity : IPipelineStep
{
    private readonly TimeSpan _slidingAffinityWindow;

    public SingleNodeAffinity(TimeSpan slidingAffinityWindow)
    {
        _slidingAffinityWindow = slidingAffinityWindow;
        _cache = new(new MemoryCacheOptions());
    }

    private readonly MemoryCache _cache;

    public async Task<AICentralResponse> Handle(IRequestContext context, IncomingCallDetails aiCallInformation,
        NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        var incomingHeader = context.RequestHeaders["x-aicentral-affinity-key"];

        if (incomingHeader.Count == 0 || string.IsNullOrWhiteSpace(incomingHeader.ToString()))
        {
            return
                new AICentralResponse(
                    DownstreamUsageInformation.Empty(
                        context,
                        aiCallInformation,
                        null,
                        null), Results.BadRequest(new
                    {
                        message =
                            "Please supply a x-aicentral-affinity-key header for something that identifies a session with an affinity endpoint"
                    }));
        }

        var key = $"{context.UserName}-{incomingHeader.ToString()}";

        if (_cache.TryGetValue(key, out var preferredEndpoint))
        {
            aiCallInformation = aiCallInformation with { PreferredEndpoint = (string)preferredEndpoint! };
        }

        var response = await next(context, aiCallInformation, cancellationToken);

        if ((response.DownstreamUsageInformation.Success ?? false) &&
            response.DownstreamUsageInformation.InternalEndpointName != null)
        {
            _cache.Set(
                key,
                response.DownstreamUsageInformation.InternalEndpointName,
                new MemoryCacheEntryOptions()
                {
                    Size = 1,
                    SlidingExpiration = _slidingAffinityWindow
                });
        }

        return response;
    }

    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}