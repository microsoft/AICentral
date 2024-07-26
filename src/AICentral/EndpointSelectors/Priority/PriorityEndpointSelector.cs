using AICentral.Core;
using Microsoft.Extensions.Primitives;

namespace AICentral.EndpointSelectors.Priority;

public class PriorityEndpointSelector : IEndpointSelector
{
    private readonly System.Random _rnd = new(Environment.TickCount);
    private readonly IEndpointDispatcher[] _prioritisedOpenAIEndpoints;
    private readonly IEndpointDispatcher[] _fallbackOpenAIEndpoints;

    public PriorityEndpointSelector(
        IEndpointDispatcher[] prioritisedOpenAIEndpoints,
        IEndpointDispatcher[] fallbackOpenAIEndpoints)
    {
        _prioritisedOpenAIEndpoints = prioritisedOpenAIEndpoints;
        _fallbackOpenAIEndpoints = fallbackOpenAIEndpoints;
    }

    public async Task<AICentralResponse> Handle(
        IRequestContext context,
        IncomingCallDetails aiCallInformation,
        bool isLastChance,
        IResponseGenerator responseGenerator,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<PriorityEndpointSelector>>();
        try
        {
            logger.LogDebug("Prioritised Endpoint selector handling request");
            return await Handle(context, aiCallInformation, cancellationToken, _prioritisedOpenAIEndpoints, false, responseGenerator);
        }
        catch (HttpRequestException e)
        {
            try
            {
                logger.LogWarning(e, "Prioritised Endpoint selector failed with primary. Trying fallback servers");
                return await Handle(context, aiCallInformation, cancellationToken, _fallbackOpenAIEndpoints, isLastChance, responseGenerator);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to handle request. Exhausted endpoints");
                throw;
            }
        }
    }

    public IEnumerable<IEndpointDispatcher> ContainedEndpoints()
    {
        return _fallbackOpenAIEndpoints.Concat(_prioritisedOpenAIEndpoints);
    }

    private async Task<AICentralResponse> Handle(
        IRequestContext context,
        IncomingCallDetails aiCallInformation,
        CancellationToken cancellationToken,
        IEndpointDispatcher[] endpoints,
        bool isLastChance,
        IResponseGenerator responseGenerator
        )
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<PriorityEndpointSelector>>();
        var toTry = endpoints.ToList();
        do
        {
            var chosen = toTry.ElementAt(_rnd.Next(0, toTry.Count));
            toTry.Remove(chosen);
            try
            {
                return
                    await chosen.Handle(
                        context,
                        aiCallInformation,
                        isLastChance,
                        responseGenerator,
                        cancellationToken); //awaiting to unwrap any Aggregate Exceptions
            }
            catch (HttpRequestException e)
            {
                if (!toTry.Any())
                {
                    logger.LogError(e, "Failed to handle request. Exhausted endpoints");
                    throw;
                }

                logger.LogWarning(e, "Failed to handle request. Trying another endpoint");
            }
        } while (toTry.Count > 0);

        throw new InvalidOperationException("Failed to satisfy request");
    }
    
    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders)
    {
        rawHeaders.Remove("x-ratelimit-remaining-tokens");
        rawHeaders.Remove("x-ratelimit-remaining-requests");
        return Task.CompletedTask;
    }
    
}