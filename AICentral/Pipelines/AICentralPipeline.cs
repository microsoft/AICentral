using System.Diagnostics;
using AICentral.Configuration;
using AICentral.Pipelines.Auth;
using AICentral.Pipelines.Endpoints;
using AICentral.Pipelines.EndpointSelectors;
using AICentral.Pipelines.RateLimiting;
using AICentral.Pipelines.Routes;

namespace AICentral.Pipelines;

public class AICentralPipeline
{
    private readonly IAICentralRouter _router;
    private readonly IAICentralAspNetCoreMiddlewarePlugin _localRateLimiting;
    private readonly IAICentralAspNetCoreMiddlewarePlugin _authProviderProvider;
    private readonly string _name;
    private readonly IList<IAICentralPipelineStep> _pipelineSteps;
    private readonly IAICentralEndpointSelector _endpointSelector;

    public AICentralPipeline(
        string name,
        IAICentralRouter router,
        IAICentralRateLimitingProvider localRateLimiting,
        IAICentralClientAuthProvider authProviderProvider,
        IList<IAICentralPipelineStep> pipelineSteps,
        IAICentralEndpointSelector endpointSelector)
    {
        _router = router;
        _localRateLimiting = localRateLimiting;
        _authProviderProvider = authProviderProvider;
        _name = name;
        _pipelineSteps = pipelineSteps;
        _endpointSelector = endpointSelector;
    }

    public async Task<AICentralResponse> Execute(HttpContext context, CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<AICentralPipeline>>();
        using var scope = logger.BeginScope(new
        {
            TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
        });

        logger.LogInformation("Executing Pipeline {PipelineName}", _name);
        var executor = new AICentralPipelineExecutor(_pipelineSteps, _endpointSelector);
        var result = await executor.Next(context, cancellationToken);
        logger.LogInformation("Executed Pipeline {PipelineName}", _name);
        return result;
    }

    public object WriteDebug()
    {
        return new
        {
            Name = _name,
            PathMatch = _router.WriteDebug(),
            RateLimiting = _localRateLimiting.WriteDebug(),
            ClientAuth = _authProviderProvider.WriteDebug(),
            Steps = _pipelineSteps.Select(x => x.WriteDebug()),
            Endpoint = _endpointSelector.WriteDebug()
        };
    }

    public void AddServices(IServiceCollection services)
    {
        _authProviderProvider.RegisterServices(services);
        _localRateLimiting.RegisterServices(services);
    }

    public void MapRoutes(WebApplication webApplication, ILogger<Configuration.AICentral> logger)
    {
        logger.LogInformation("Mapping route for Pipeline {Pipeline}", _name);
        
        var route = _router.BuildRoute(webApplication, async (HttpContext ctx, CancellationToken token) => (await Execute(ctx, token)).ResultHandler);

        _authProviderProvider.ConfigureRoute(webApplication, route);
        _localRateLimiting.ConfigureRoute(webApplication, route);
    }
}