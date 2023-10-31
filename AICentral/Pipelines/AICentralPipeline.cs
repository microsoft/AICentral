using System.Diagnostics;
using AICentral.Pipelines.Auth;
using AICentral.Pipelines.EndpointSelectors;
using AICentral.Pipelines.Routes;

namespace AICentral.Pipelines;

public class AICentralPipeline
{
    private readonly string _name;
    private readonly IAICentralRouter _router;
    private readonly IAICentralClientAuthRuntime _authProviderProvider;
    private readonly IList<IAICentralPipelineStepRuntime> _pipelineSteps;
    private readonly IAICentralEndpointSelectorRuntime _endpointSelector;

    public AICentralPipeline(
        string name,
        IAICentralRouter router,
        IAICentralClientAuthRuntime authProviderProvider,
        IList<IAICentralPipelineStepRuntime> pipelineSteps,
        IAICentralEndpointSelectorRuntime endpointSelector)
    {
        _name = name;
        _router = router;
        _authProviderProvider = authProviderProvider;
        _pipelineSteps = pipelineSteps.Select(x => x).ToArray();
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
            ClientAuth = _authProviderProvider.WriteDebug(),
            Steps = _pipelineSteps.Select(x => x.WriteDebug()),
            Endpoint = _endpointSelector.WriteDebug()
        };
    }

    public void BuildRoute(WebApplication webApplication)
    {
        _router.BuildRoute(webApplication, Execute);
    }
}