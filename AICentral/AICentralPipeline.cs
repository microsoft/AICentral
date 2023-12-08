using System.Diagnostics;
using AICentral.Core;
using AICentral.Steps.Auth;
using AICentral.Steps.EndpointSelectors;
using AICentral.Steps.EndpointSelectors.Single;
using AICentral.Steps.Routes;

namespace AICentral;

public class AICentralPipeline
{
    private readonly string _name;
    private readonly HeaderMatchRouter _router;
    private readonly IAICentralClientAuthFactory _clientAuthStep;
    private readonly IList<IAICentralGenericStepFactory> _pipelineSteps;
    private readonly IAICentralEndpointSelectorFactory _endpointSelector;

    public AICentralPipeline(
        string name,
        HeaderMatchRouter router,
        IAICentralClientAuthFactory clientAuthStep,
        IAICentralGenericStepFactory[] pipelineSteps,
        IAICentralEndpointSelectorFactory endpointSelector)
    {
        _name = name;
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

        var detector = context.RequestServices.GetRequiredService<IncomingCallDetector>();
        var requestDetails = await detector.Detect(context.Request, cancellationToken);

        logger.LogDebug("Detected {RequestType} / {CallType} from incoming request",
            requestDetails.IncomingCallDetails.ServiceType, requestDetails.IncomingCallDetails.AICallType);

        IEndpointSelector? endpointSelector;
        if (requestDetails.IncomingCallDetails.AICallType == AICallType.Other)
        {
            endpointSelector = FindAffinityServer(requestDetails) ?? _endpointSelector.Build();
        }
        else
        {
            endpointSelector = _endpointSelector.Build();
        }

        using var executor = new AICentralPipelineExecutor(_pipelineSteps.Select(x => x.Build()), endpointSelector!);
        var result = await executor.Next(context, requestDetails, cancellationToken);
        logger.LogInformation("Executed Pipeline {PipelineName}", _name);
        return result;
    }

    private IEndpointSelector? FindAffinityServer(AICallInformation requestDetails)
    {
        var availableEndpointSelectors = AffinityEndpointHelper.FlattenedEndpoints(_endpointSelector.Build());
        AffinityEndpointHelper.IsAffinityRequest(requestDetails, availableEndpointSelectors,
            out var affinityEndpointSelector);
        return affinityEndpointSelector;
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
                requestDetails.IncomingCallDetails.AICallType,
                requestDetails.IncomingCallDetails.PromptText,
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