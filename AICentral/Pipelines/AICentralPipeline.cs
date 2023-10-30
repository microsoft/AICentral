using System.Diagnostics;
using AICentral.Pipelines.Auth;
using AICentral.Pipelines.EndpointSelectors;
using AICentral.Pipelines.Routes;

namespace AICentral.Pipelines;

public class AICentralPipeline
{
    private readonly IAICentralRouter _router;
    private readonly IAICentralPipelineStepRuntime _authProviderProvider;
    private readonly string _name;
    private readonly IList<IAICentralPipelineStepRuntime> _pipelineSteps;
    private readonly IAICentralEndpointSelectorRuntime _endpointSelector;
    private readonly IAICentralClientAuthProvider _ap1;
    private readonly IList<IAICentralPipelineStep<IAICentralPipelineStepRuntime>> _ps1;
    private readonly IAICentralEndpointSelector _es1;

    public AICentralPipeline(
        string name,
        IAICentralRouter router,
        IAICentralClientAuthProvider authProviderProvider,
        IList<IAICentralPipelineStep<IAICentralPipelineStepRuntime>> pipelineSteps,
        IAICentralEndpointSelector endpointSelector)
    {
        _router = router;
        _ap1 = authProviderProvider;
        _authProviderProvider = authProviderProvider.Build();
        _name = name;
        _ps1 = pipelineSteps;
        _pipelineSteps = pipelineSteps.Select(x => x.Build()).ToArray();
        _es1 = endpointSelector;
        _endpointSelector = endpointSelector.Build();
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
            ClientAuth = _authProviderProvider.WriteDebug(),
            Steps = _pipelineSteps.Select(x => x.WriteDebug()),
            Endpoint = _endpointSelector.WriteDebug()
        };
    }

    public void AddServices(IServiceCollection services)
    {
        _ap1.RegisterServices(services);
        foreach(var step in _ps1) step.RegisterServices(services);
        _es1.RegisterServices(services);
    }

    public void MapRoutes(WebApplication webApplication, ILogger<Configuration.AICentral> logger)
    {
        logger.LogInformation("Mapping route for Pipeline {Pipeline}", _name);
        
        var route = _router.BuildRoute(webApplication, async (HttpContext ctx, CancellationToken token) => (await Execute(ctx, token)).ResultHandler);

        _ap1.ConfigureRoute(webApplication, route);
        foreach(var step in _ps1) step.ConfigureRoute(webApplication, route);
    }
}