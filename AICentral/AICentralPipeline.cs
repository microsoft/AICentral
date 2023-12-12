using System.Diagnostics;
using System.Diagnostics.Metrics;
using AICentral.Auth;
using AICentral.Core;
using AICentral.EndpointSelectors;
using AICentral.Routes;

namespace AICentral;

/// <summary>
/// Represents a Pipeline. This class is the main entry path for a request after it's been matched by a route.
/// It's a stateless class which emits telemetry, but the main work of executing steps is performed by the
/// AICentralPipelineExecutor class. An instance of AICentralPipelineExecutor is created to encapsulate each request to OpenAI.
/// </summary>
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

    private static readonly Histogram<int> TokenMeter =
        AICentralActivitySource.AICentralMeter.CreateHistogram<int>("aicentral.tokens.sum");

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

    /// <summary>
    /// Orchestrates the request through the pipeline. This method is called by the route handler.
    /// This method ultimately creates an instance of AICentralPipelineExecutor to execute the request.
    /// </summary>
    /// <remarks>
    /// If an affinity header is detected to a non chat-like endpoint, we will switch the EndpointSelector to one
    /// containing only that downstream server.
    /// </remarks>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<AICentralResponse> Execute(HttpContext context, CancellationToken cancellationToken)
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

            var tagList = new TagList
            {
                { "Model", result.AICentralUsageInformation.ModelName },
                { "Endpoint", result.AICentralUsageInformation.OpenAIHost }
            };
            
            DurationMeter.Record(result.AICentralUsageInformation.Duration.TotalMilliseconds, tagList);

            if (result.AICentralUsageInformation.TotalTokens != null)
            {
                TokenMeter.Record(result.AICentralUsageInformation.TotalTokens.Value, tagList);
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