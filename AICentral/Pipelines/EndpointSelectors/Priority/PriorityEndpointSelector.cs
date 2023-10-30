using AICentral.Pipelines.Endpoints;
using AICentral.Pipelines.EndpointSelectors.Random;

namespace AICentral.Pipelines.EndpointSelectors.Priority;

public class PriorityEndpointSelector : IAICentralEndpointSelector, IAICentralEndpointSelectorRuntime
{
    private readonly System.Random _rnd = new(Environment.TickCount);
    private readonly RandomEndpointSelector _prioritisedOpenAiEndpoints;
    private readonly RandomEndpointSelector _fallbackOpenAiEndpoints;

    public PriorityEndpointSelector(RandomEndpointSelector prioritisedOpenAiEndpoints,
        RandomEndpointSelector fallbackOpenAiEndpoints)
    {
        _prioritisedOpenAiEndpoints = prioritisedOpenAiEndpoints;
        _fallbackOpenAiEndpoints = fallbackOpenAiEndpoints;
    }

    public async Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<RandomEndpointSelector>>();
        try
        {
            logger.LogDebug("Prioritised Endpoint selector handling request");
            return await _prioritisedOpenAiEndpoints.Handle(context, pipeline, cancellationToken);
        }
        catch (Exception)
        {
            try
            {
                logger.LogWarning("e, Prioritised Endpoint selector failed with primary. Trying fallback servers");
                return await _fallbackOpenAiEndpoints.Handle(context, pipeline, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to handle request. Exhausted endpoints");
                throw new InvalidOperationException("No available Open AI hosts", e);
            }
        }
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "Priority Router",
            PrioritisedEndpoints = _prioritisedOpenAiEndpoints.WriteDebug(),
            FallbackEndpoints = _fallbackOpenAiEndpoints.WriteDebug(),
        };
    }

    public IAICentralEndpointSelectorRuntime Build()
    {
        return this;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

    public static string ConfigName => "Prioritised";

    public static IAICentralEndpointSelector BuildFromConfig(Dictionary<string, string> parameters,
        Dictionary<string, IAICentralEndpointRuntime> endpoints)
    {
        return new PriorityEndpointSelector(
            new RandomEndpointSelector(
                parameters["PrioritisedEndpoints"].Split(',').Select(x => endpoints[x]).ToArray()),
            new RandomEndpointSelector(parameters["FallbackEndpoints"].Split(',').Select(x => endpoints[x]).ToArray()));
    }
}