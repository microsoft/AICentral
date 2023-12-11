using System.Diagnostics;
using System.Diagnostics.Metrics;
using AICentral.Core;
using AICentral.Steps.Auth;
using AICentral.Steps.EndpointSelectors;
using AICentral.Steps.Routes;

namespace AICentral;

public class AICentralPipeline
{
    private readonly string _name;
    private readonly HeaderMatchRouter _router;
    private readonly IAICentralClientAuthFactory _clientAuthStep;
    private readonly IList<IAICentralGenericStepFactory> _pipelineSteps;
    private readonly IAICentralEndpointSelectorFactory _endpointSelector;

    private static readonly Counter<int> RequestMeter =
        AICentralActivitySource.AICentralMeter.CreateCounter<int>("aicentral.requests.count");

    private static readonly Counter<int> FailedRequestMeter =
        AICentralActivitySource.AICentralMeter.CreateCounter<int>("aicentral.failedrequests.count");

    private static readonly Counter<int> TokenMeter =
        AICentralActivitySource.AICentralMeter.CreateCounter<int>("aicentral.tokens.sum");

    private static readonly Counter<int> SuccessRequestMeter =
        AICentralActivitySource.AICentralMeter.CreateCounter<int>("aicentral.successfulrequests.count");

    private static readonly Histogram<double> DurationMeter =
        AICentralActivitySource.AICentralMeter.CreateHistogram<double>("aicentral.downstreamrequest.duration");

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
        // Create a new Activity scoped to the method
        using var activity = AICentralActivitySource.AICentralRequestActivitySource.StartActivity("AICentalRequest");

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

        IAICentralEndpointSelector? endpointSelector;
        if (requestDetails.IncomingCallDetails.AICallType == AICallType.Other)
        {
            endpointSelector = FindAffinityServer(requestDetails) ?? _endpointSelector.Build();
        }
        else
        {
            endpointSelector = _endpointSelector.Build();
        }


        using var executor = new AICentralPipelineExecutor(_pipelineSteps.Select(x => x.Build()), endpointSelector);
        RequestMeter.Add(1);
        try
        {
            var result = await executor.Next(context, requestDetails, cancellationToken);
            logger.LogInformation("Executed Pipeline {PipelineName}", _name);
            SuccessRequestMeter.Add(1);

            var tagList = new TagList();
            tagList.Add("Model", result.AICentralUsageInformation.ModelName);
            tagList.Add("Endpoint", result.AICentralUsageInformation.OpenAIHost);
            DurationMeter.Record(result.AICentralUsageInformation.Duration.TotalMilliseconds, tagList);

            if (result.AICentralUsageInformation.TotalTokens != null)
            {
                TokenMeter.Add(result.AICentralUsageInformation.TotalTokens.Value, tagList);
            }

            activity?.AddTag("AICentral.Duration", result.AICentralUsageInformation.Duration);
            activity?.AddTag("AICentral.Model", result.AICentralUsageInformation.ModelName);
            activity?.AddTag("AICentral.CallType", result.AICentralUsageInformation.CallType);
            activity?.AddTag("AICentral.TotalTokens", result.AICentralUsageInformation.TotalTokens);
            activity?.AddTag("AICentral.OpenAIHost", result.AICentralUsageInformation.OpenAIHost);

            return result;
        }
        catch
        {
            FailedRequestMeter.Add(1);
            throw;
        }
    }

    private IAICentralEndpointSelector? FindAffinityServer(AICallInformation requestDetails)
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
        var dateTimeProvider = context.RequestServices.GetRequiredService<IDateTimeProvider>();
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
                dateTimeProvider.Now,
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