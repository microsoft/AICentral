using AICentral.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace AICentral.Affinity;

public class Affinity : IPipelineStep
{
    private readonly TimeSpan _slidingAffinityWindow;

    public Affinity(TimeSpan slidingAffinityWindow)
    {
        _slidingAffinityWindow = slidingAffinityWindow;
        _cache = new(new MemoryCacheOptions());
    }

    private readonly MemoryCache _cache;

    public async Task<AICentralResponse> Handle(HttpContext context, IncomingCallDetails aiCallInformation,
        NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        var userId = context.User.Identity?.Name;
        if (userId != null)
        {
            if (_cache.TryGetValue(userId, out var preferredEndpoint))
            {
                aiCallInformation = aiCallInformation with { PreferredEndpoint = (string)preferredEndpoint! };
            }

            var response = await next(context, aiCallInformation, cancellationToken);
            
            if ((response.DownstreamUsageInformation.Success ?? false) &&
                response.DownstreamUsageInformation.InternalEndpointName != null)
            {
                _cache.Set(
                    userId,
                    response.DownstreamUsageInformation.InternalEndpointName,
                    new MemoryCacheEntryOptions()
                    {
                        Size = 1,
                        SlidingExpiration = _slidingAffinityWindow
                    });
            }

            return response;
        }

        return await next(context, aiCallInformation, cancellationToken);
    }

    public Task BuildResponseHeaders(HttpContext context, HttpResponseMessage rawResponse,
        Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
}