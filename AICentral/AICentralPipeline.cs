using System.Diagnostics;
using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Auth;
using AICentral.PipelineComponents.EndpointSelectors;
using AICentral.PipelineComponents.Routes;

namespace AICentral;

public class AICentralPipeline
{
    private readonly string _name;
    private readonly PathMatchRouter _router;
    private readonly IAICentralClientAuthStep _clientAuthStep;
    private readonly IList<IAICentralPipelineStep> _pipelineSteps;
    private readonly IEndpointSelector _endpointSelector;
    private readonly IIncomingCallExtractor _incomingCallExtractor;

    public AICentralPipeline(
        EndpointType endpointType,
        string name,
        PathMatchRouter router,
        IAICentralClientAuthStep clientAuthStep,
        IList<IAICentralPipelineStep> pipelineSteps,
        IEndpointSelector endpointSelector)
    {
        _name = name;
        _incomingCallExtractor = endpointType switch
        {
            EndpointType.AzureOpenAI => new AzureOpenAiCallInformationExtractor(),
            EndpointType.OpenAI => new OpenAiCallInformationExtractor(),
            _ => throw new InvalidOperationException("Unsupported Pipeline type")
        };
        _router = router;
        _clientAuthStep = clientAuthStep;
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
        var requestDetails = await _incomingCallExtractor.Extract(context.Request, cancellationToken);
        var executor = new AICentralPipelineExecutor(_pipelineSteps, _endpointSelector);
        var result = await executor.Next(context, requestDetails, cancellationToken);
        logger.LogInformation("Executed Pipeline {PipelineName}", _name);
        return result;
    }

    public object WriteDebug()
    {
        return new
        {
            Name = _name,
            PathMatch = _router.WriteDebug(),
            ClientAuth = _clientAuthStep.WriteDebug(),
            Steps = _pipelineSteps.Select(x => x.WriteDebug()),
            EndpointSelector = _endpointSelector.WriteDebug()
        };
    }

    public void BuildRoute(WebApplication webApplication)
    {
        var route = _router.BuildRoute(webApplication,
            async (HttpContext ctx, CancellationToken token) => (await Execute(ctx, token)).ResultHandler);
        _clientAuthStep.ConfigureRoute(webApplication, route);
        foreach (var step in _pipelineSteps) step.ConfigureRoute(webApplication, route);
    }
}