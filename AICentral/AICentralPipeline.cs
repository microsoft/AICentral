using System.Diagnostics;
using AICentral.Configuration.JSON;
using AICentral.Steps;
using AICentral.Steps.Auth;
using AICentral.Steps.EndpointSelectors;
using AICentral.Steps.EndpointSelectors.Single;
using AICentral.Steps.Routes;

namespace AICentral;

public class AICentralPipeline
{
    private readonly string _name;
    private readonly HeaderMatchRouter _router;
    private readonly IAICentralClientAuthStep _clientAuthStep;
    private readonly IList<IAICentralPipelineStep> _pipelineSteps;
    private readonly IEndpointSelector _endpointSelector;
    private readonly IIncomingCallExtractor _incomingCallExtractor;

    public AICentralPipeline(
        EndpointType endpointType,
        string name,
        HeaderMatchRouter router,
        IAICentralClientAuthStep clientAuthStep,
        IList<IAICentralPipelineStep> pipelineSteps,
        IEndpointSelector endpointSelector)
    {
        _name = name;
        _incomingCallExtractor = endpointType switch
        {
            EndpointType.AzureOpenAI => new AzureOpenAiCallInformationExtractor(),
            EndpointType.OpenAI => new OpenAICallInformationExtractor(),
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

        if (requestDetails.AICallType == AICallType.Other && !(_endpointSelector is SingleEndpointSelector))
        {
            return UnableToProxyUnknownCallTypesToMultiNodeClusters(context, requestDetails);
        }

        using var executor = new AICentralPipelineExecutor(_pipelineSteps, _endpointSelector);
        var result = await executor.Next(context, requestDetails, cancellationToken);
        logger.LogInformation("Executed Pipeline {PipelineName}", _name);
        return result;
    }

    /// <summary>
    /// safety first - Azure Open AI uses a async model for images which would need affinity. We could build this 
    /// in, but for now, let's return a bad-request
    /// </summary>
    /// <param name="context"></param>
    /// <param name="requestDetails"></param>
    /// <returns></returns>
    private static AICentralResponse UnableToProxyUnknownCallTypesToMultiNodeClusters(HttpContext context,
        AICallInformation requestDetails)
    {
        return new AICentralResponse(
            new AICentralUsageInformation(
                string.Empty,
                string.Empty,
                context.User.Identity?.Name ?? "unknown",
                requestDetails.AICallType,
                requestDetails.PromptText,
                string.Empty,
                0,
                0,
                0,
                0,
                0,
                context.Connection.RemoteIpAddress?.ToString() ?? "",
                DateTimeOffset.Now,
                TimeSpan.Zero),
            Results.BadRequest(new { reason = "Unable to proxy 'other' calls to an endpoint cluster." }));
    }

    public object WriteDebug()
    {
        return new
        {
            Name = _name,
            RouteMatch = _router.WriteDebug(),
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