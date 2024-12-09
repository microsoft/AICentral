using AICentral.Core;
using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace AICentral.Dapr.Broadcast;

public class DaprBroadcaster : IPipelineStep
{
    private readonly DaprBroadcastOptions _properties;
    private readonly string _id;

    internal DaprBroadcaster(string id, DaprBroadcastOptions properties)
    {
        _properties = properties;
        _id = id;
    }

    public async Task<AICentralResponse> Handle(IRequestContext context, IncomingCallDetails aiCallInformation, NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        var response = await next(context, aiCallInformation, cancellationToken);

        var logger = context.RequestServices.GetRequiredService<ILogger<DaprBroadcaster>>();
        var daprClient = context.RequestServices.GetRequiredKeyedService<DaprClient>(_id);

        await daprClient.PublishEventAsync(
            _properties.DaprPubSubComponentName,
            _properties.PubSubTopicName,
            new LogEntry(
                Guid.NewGuid().ToString(),
                response.DownstreamUsageInformation.InternalEndpointName,
                response.DownstreamUsageInformation.OpenAIHost,
                response.DownstreamUsageInformation.ModelName,
                response.DownstreamUsageInformation.DeploymentName,
                response.DownstreamUsageInformation.Client,
                response.DownstreamUsageInformation.CallType.ToString(),
                response.DownstreamUsageInformation.StreamingResponse,
                response.DownstreamUsageInformation.RawPrompt,
                response.DownstreamUsageInformation.Prompt,
                response.DownstreamUsageInformation.Response,
                response.DownstreamUsageInformation.EstimatedTokens?.Value.EstimatedPromptTokens,
                response.DownstreamUsageInformation.EstimatedTokens?.Value.EstimatedCompletionTokens,
                response.DownstreamUsageInformation.KnownTokens?.PromptTokens,
                response.DownstreamUsageInformation.KnownTokens?.CompletionTokens,
                response.DownstreamUsageInformation.TotalTokens,
                response.DownstreamUsageInformation.RemoteIpAddress,
                response.DownstreamUsageInformation.StartDate,
                response.DownstreamUsageInformation.Duration,
                response.DownstreamUsageInformation.Success
            ), cancellationToken);
        
        logger.LogDebug("Raised audit event for {0} to {1}", _properties.PubSubTopicName, _properties.DaprPubSubComponentName);

        return response;
    }

    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }
    
}