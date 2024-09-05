using System.Diagnostics;
using AICentral.Core;
using AICentral.EndpointSelectors;
using Microsoft.Extensions.Primitives;

namespace AICentral;

/// <summary>
/// Represents a Pipeline. This class is the main entry path for a request after it's been matched by a route.
/// It's a stateless class which emits telemetry, but the main work of executing steps is performed by the
/// AICentralPipelineExecutor class. An instance of AICentralPipelineExecutor is created to encapsulate each request to OpenAI.
/// </summary>
public class Pipeline
{
    private readonly string _name;
    private readonly HostNameMatchRouter _router;
    private readonly IPipelineStepFactory _clientAuthStep;
    private readonly IPipelineStepFactory[] _pipelineSteps;
    private readonly IEndpointSelectorFactory _endpointSelector;
    private readonly IRouteProxy[] _routeProxies;
    private readonly OTelConfig _openTelemetryConfig;
    private readonly bool _enableDiagnosticsHeaders;

    public const string XAiCentralStreamingTokenHeader = "x-aicentral-streaming-tokens";

    public Pipeline(
        string name,
        HostNameMatchRouter router,
        IPipelineStepFactory clientAuthStep,
        IPipelineStepFactory[] pipelineSteps,
        IEndpointSelectorFactory endpointSelector,
        IRouteProxy[] routeProxies,
        OTelConfig openTelemetryConfig,
        bool enableDiagnosticsHeaders)
    {
        _name = name;
        _router = router;
        _clientAuthStep = clientAuthStep;
        _pipelineSteps = new[] { _clientAuthStep }.Union(pipelineSteps.Select(x => x)).ToArray();
        _endpointSelector = endpointSelector;
        _routeProxies = routeProxies;
        _openTelemetryConfig = openTelemetryConfig;
        _enableDiagnosticsHeaders = enableDiagnosticsHeaders;
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
    /// <param name="assistantName"></param>
    /// <param name="callType"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="deploymentName"></param>
    /// <returns></returns>
    public async Task<AICentralResponse> Execute(
        IRequestContext context, 
        string? deploymentName, 
        string? assistantName,
        AICallType callType, 
        CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        sw.Start();

        // Create a new Activity scoped to the method
        using var activity = ActivitySource.AICentralRequestActivitySource.StartActivity("AICentralRequest");
        if (_enableDiagnosticsHeaders)
        {
            context.ResponseHeaders.TryAdd("x-aicentral-pipeline", new StringValues(_name));
        }

        var logger = context.GetLogger<Pipeline>();
        using var scope = logger.BeginScope(new
        {
            TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
        });

        logger.LogInformation("Executing Pipeline {PipelineName}", _name);

        var requestDetails = await new AzureOpenAIDetector().Detect(_name, deploymentName, assistantName, callType,
            context, cancellationToken);

        logger.LogDebug("Detected {CallType} from incoming request",
            requestDetails.AICallType);

        using var executor = new PipelineExecutor(_pipelineSteps.Select(x => x.Build(context.RequestServices)),
            FindEndpointSelectorOrAffinityServer);

        var requestTagList = new TagList
        {
            { "Deployment", requestDetails.IncomingModelName },
            { "Pipeline", _name },
        };
        if (_openTelemetryConfig.Transmit.GetValueOrDefault())
        {
            ActivitySources.RecordUpDownCounter($"activeRequests", "requests", 1, requestTagList);
        }

        try
        {
            if (requestDetails.AICallResponseType == AICallResponseType.Streaming &&
                context.ResponseSupportsTrailers())
            {
                context.ResponseDeclareTrailer(XAiCentralStreamingTokenHeader);
            }

            try
            {
                var result = await executor.Next(context, requestDetails, cancellationToken);

                sw.Stop();

                logger.LogInformation("Executed Pipeline {PipelineName}", _name);

                if (result.DownstreamUsageInformation.StreamingResponse.GetValueOrDefault() &&
                    result.DownstreamUsageInformation.EstimatedTokens?.Value.EstimatedCompletionTokens != null &&
                    context.ResponseSupportsTrailers())
                {
                    var streamingTokenCount =
                        result.DownstreamUsageInformation.EstimatedTokens!.Value.EstimatedCompletionTokens!.ToString();

                    context.ResponseAppendTrailer(
                        XAiCentralStreamingTokenHeader,
                        streamingTokenCount!);
                }

                TransmitOtelTelemetry(context, result, sw, activity);

                return result;
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Failed to handle request");
                return new AICentralResponse(
                    DownstreamUsageInformation.Empty(context, requestDetails, null, null),
                    Results.StatusCode(502));
            }
        }
        finally
        {
            if (_openTelemetryConfig.Transmit.GetValueOrDefault())
            {
                ActivitySources.RecordUpDownCounter("activeRequests", "requests", -1, requestTagList);
            }
        }
    }

    private void TransmitOtelTelemetry(IRequestContext context, AICentralResponse result, Stopwatch sw, Activity? activity)
    {
        if (!_openTelemetryConfig.Transmit.GetValueOrDefault()) return;

        var tagList = new TagList
        {
            { "Deployment", result.DownstreamUsageInformation.DeploymentName },
            { "Model", result.DownstreamUsageInformation.ModelName },
            { "Endpoint", result.DownstreamUsageInformation.OpenAIHost },
            { "Success", result.DownstreamUsageInformation.Success },
            { "Streaming", result.DownstreamUsageInformation.StreamingResponse },
            { "Pipeline", _name },
        };

        if (_openTelemetryConfig.AddClientNameTag.GetValueOrDefault())
        {
            tagList.Add("ClientName", context.UserName ?? "unknown");
        }

        ActivitySources.RecordHistogram(
            $"request.duration",
            "ms",
            sw.ElapsedMilliseconds, tagList);

        if (result.DownstreamUsageInformation.TotalTokens != null)
        {
            ActivitySources.RecordHistogram(
                $"request.tokens_consumed", "tokens",
                result.DownstreamUsageInformation.TotalTokens.Value, tagList);
        }
        if (result.DownstreamUsageInformation.PromptTokens != null) {
            ActivitySources.RecordHistogram(
                $"request.prompt_tokens_consumed", "tokens",
                result.DownstreamUsageInformation.PromptTokens.Value, tagList);
        }

        if (result.DownstreamUsageInformation.CompletionTokens != null)
        {
            ActivitySources.RecordHistogram(
                $"request.completion_tokens_consumed", "tokens",
                result.DownstreamUsageInformation.CompletionTokens.Value, tagList);
        }

        var downsteamMetadata = result.DownstreamUsageInformation.ResponseMetadata;
        if (downsteamMetadata != null)
        {
            var modelOrDeployment = result.DownstreamUsageInformation.DeploymentName ??
                                    result.DownstreamUsageInformation.ModelName ??
                                    "";

            var normalisedHostName = result.DownstreamUsageInformation.OpenAIHost?.Replace(".", "_") ?? string.Empty;

            if (downsteamMetadata.RemainingTokens != null)
            {
                //Gauges don't transmit custom dimensions so I need a new metric name for each host / deployment pair.
                ActivitySources.RecordGaugeMetric(
                    $"downstream.{normalisedHostName}.{modelOrDeployment}.tokens_remaining", "tokens",
                    downsteamMetadata.RemainingTokens.Value);
            }

            if (downsteamMetadata.RemainingRequests != null)
            {
                //Gauges don't transmit custom dimensions so I need a new metric name for each host / deployment pair.
                ActivitySources.RecordGaugeMetric(
                    $"downstream.{normalisedHostName}.{modelOrDeployment}.requests_remaining", "tokens",
                    downsteamMetadata.RemainingRequests.Value);
            }
        }

        ActivitySources.RecordHistogram(
            "downstream.duration",
            "ms", result.DownstreamUsageInformation.Duration.TotalMilliseconds,
            tagList);

        activity?.AddTag("AICentral.Duration", sw.ElapsedMilliseconds);
        activity?.AddTag("AICentral.Downstream.Duration",
            result.DownstreamUsageInformation.Duration.TotalMilliseconds);
        activity?.AddTag("AICentral.Deployment", result.DownstreamUsageInformation.DeploymentName);
        activity?.AddTag("AICentral.Model", result.DownstreamUsageInformation.ModelName);
        activity?.AddTag("AICentral.CallType", result.DownstreamUsageInformation.CallType);
        activity?.AddTag("AICentral.TotalTokens", result.DownstreamUsageInformation.TotalTokens);
        activity?.AddTag("AICentral.OpenAIHost", result.DownstreamUsageInformation.OpenAIHost);
        activity?.AddTag("AICentral.Streaming", result.DownstreamUsageInformation.StreamingResponse);
        activity?.AddTag("AICentral.Pipeline", _name);
    }

    private IEndpointSelector FindEndpointSelectorOrAffinityServer(IncomingCallDetails requestDetails)
    {
        return FindAffinityServer(requestDetails) ?? _endpointSelector.Build();
    }

    private IEndpointSelector? FindAffinityServer(IncomingCallDetails incomingCallDetails)
    {
        if (incomingCallDetails.PreferredEndpoint == null) return null;
        return AffinityEndpointHelper.FindAffinityRequestEndpoint(
            incomingCallDetails,
            AffinityEndpointHelper.FlattenedEndpoints(
                _endpointSelector.Build())
        );
    }

    public object WriteDebug()
    {
        return new
        {
            Name = _name,
            RouteMatch = _router.WriteDebug(),
            ClientAuth = _clientAuthStep.WriteDebug(),
            Steps = _pipelineSteps.Except([_clientAuthStep]).Select(x => x.WriteDebug()),
            EndpointSelector = _endpointSelector.WriteDebug(),
            RouteProxies = _routeProxies.Select(x => x.WriteDebug()),
            OpenTelemetryConfig = _openTelemetryConfig
        };
    }

    public void BuildRoute(WebApplication webApplication)
    {
        foreach (var route in _router.BuildRoutes(webApplication, Execute, _routeProxies))
        {
            _clientAuthStep.ConfigureRoute(webApplication, route);
            foreach (var step in _pipelineSteps) step.ConfigureRoute(webApplication, route);
        }
    }
}