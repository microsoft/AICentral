using System.Diagnostics;
using System.Diagnostics.Metrics;
using AICentral.Core;
using AICentral.Steps.Auth;
using AICentral.Steps.EndpointSelectors;
using AICentral.Steps.EndpointSelectors.Single;
using AICentral.Steps.Routes;
using AICentral.Steps.TokenBasedRateLimiting;
using Newtonsoft.Json;

namespace AICentral;

public class AICentralPipeline
{
    private readonly string _name;
    private readonly HeaderMatchRouter _router;
    private readonly IAICentralClientAuthFactory _clientAuthStep;
    private readonly IList<IAICentralGenericStepFactory> _pipelineSteps;
    private readonly IAICentralEndpointSelectorFactory _endpointSelector;

    private readonly Meter _aiCentralMeter;
    private readonly Counter<int> _requestMeter;
    private readonly Counter<int> _failedRequestMeter;
    private readonly Counter<int> _tokenMeter;
    private readonly Counter<int> _successRequestMeter;
    private readonly ActivitySource _aiCentralRequestActivitySource;

    public static readonly string AICentralMeterName = typeof(AICentralPipeline).Assembly.GetName().Name!;

    private static readonly string AICentralMeterVersion =
        typeof(AICentralPipeline).Assembly.GetName().Version!.ToString();

    private readonly Histogram<double> _durationMeter;

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

        _aiCentralMeter = new Meter(AICentralMeterName, AICentralMeterVersion);
        _requestMeter = _aiCentralMeter.CreateCounter<int>("aicentral.requests.count");
        _failedRequestMeter = _aiCentralMeter.CreateCounter<int>("aicentral.failedrequests.count");
        _successRequestMeter = _aiCentralMeter.CreateCounter<int>("aicentral.successfulrequests.count");
        _tokenMeter = _aiCentralMeter.CreateCounter<int>("aicentral.tokens.sum");
        _durationMeter = _aiCentralMeter.CreateHistogram<double>("aicentral.downstreamrequest.duration");

        _aiCentralRequestActivitySource = new ActivitySource("aicentral");
    }

    public async Task<AICentralResponse> Execute(HttpContext context, CancellationToken cancellationToken)
    {
        // Create a new Activity scoped to the method
        using var activity = _aiCentralRequestActivitySource.StartActivity("AICentalRequest");

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
        _requestMeter.Add(1);
        try
        {
            var result = await executor.Next(context, requestDetails, cancellationToken);
            logger.LogInformation("Executed Pipeline {PipelineName}", _name);
            _successRequestMeter.Add(1);

            var tagList = new TagList();
            tagList.Add("Model", result.AICentralUsageInformation.ModelName);
            tagList.Add("Endpoint", result.AICentralUsageInformation.OpenAIHost);

            _durationMeter.Record(result.AICentralUsageInformation.Duration.TotalMilliseconds, tagList);

            if (result.AICentralUsageInformation.TotalTokens != null)
            {
                _tokenMeter.Add(result.AICentralUsageInformation.TotalTokens.Value, tagList);
            }

            return result;
        }
        catch
        {
            _failedRequestMeter.Add(1);
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