using AICentral.Core;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace AICentral.Dapr.Broadcast;

public class DaprBroadcaster : IPipelineStep, IPipelineStepFactory
{
    private readonly DaprBroadcastOptions _properties;
    private readonly string _id;

    private DaprBroadcaster(DaprBroadcastOptions properties)
    {
        _properties = properties;
        _id = Guid.NewGuid().ToString();
    }

    public async Task<AICentralResponse> Handle(IRequestContext context, IncomingCallDetails aiCallInformation, NextPipelineStep next,
        CancellationToken cancellationToken)
    {
        var response = await next(context, aiCallInformation, cancellationToken);
        
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

        return response;
    }

    public Task BuildResponseHeaders(IRequestContext context, HttpResponseMessage rawResponse, Dictionary<string, StringValues> rawHeaders)
    {
        return Task.CompletedTask;
    }

    public void RegisterServices(IServiceCollection services)
    {
        var clientBuilder = new DaprClientBuilder();
        if (_properties.DaprProtocol == DaprProtocol.Http)
        {
            clientBuilder = clientBuilder.UseGrpcEndpoint(_properties.DaprUri);
        }
        else
        {
            clientBuilder = clientBuilder.UseHttpEndpoint(_properties.DaprUri);
        }

        if (!string.IsNullOrWhiteSpace(_properties.DaprToken))
        {
            clientBuilder = clientBuilder.UseDaprApiToken(_properties.DaprToken);
        }
        
        services.AddKeyedSingleton(_id, clientBuilder.Build());
    }

    public IPipelineStep Build(IServiceProvider serviceProvider)
    {
        return this;
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

    public static string ConfigName => "DaprBroadcaster";
    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var properties = config.TypedProperties<DaprBroadcastOptions>();
        Guard.NotNull(properties, "Properties");
        Guard.NotNull(properties.DaprUri, nameof(properties.DaprUri));
        Guard.NotNull(properties.DaprProtocol, nameof(properties.DaprProtocol));
        Guard.NotNull(properties.DaprPubSubComponentName, nameof(properties.DaprPubSubComponentName));
        Guard.NotNull(properties.PubSubTopicName, nameof(properties.PubSubTopicName));
        return new DaprBroadcaster(properties);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "DaprBroadcaster",
            DaprHost = _properties.DaprUri,
            DaprPort = _properties.DaprProtocol,
            DaprPubSubComponentName = _properties.DaprPubSubComponentName,
            PubSubTopicName = _properties.PubSubTopicName,
        };
    }
}