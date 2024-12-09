using AICentral.Core;
using Dapr.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AICentral.Dapr.Broadcast;

public class DaprBroadcasterFactory : IPipelineStepFactory
{
    private readonly DaprBroadcastOptions _properties;
    private readonly string _id;
    private readonly DaprBroadcaster _broadcaster;

    public DaprBroadcasterFactory(DaprBroadcastOptions properties)
    {
        _properties = properties;
        _id = Guid.NewGuid().ToString();
        _broadcaster = new DaprBroadcaster(_id, properties);
    }

    public void RegisterServices(IServiceCollection services)
    {
        var clientBuilder = new DaprClientBuilder();
        if (_properties.DaprProtocol == DaprProtocol.Http)
        {
            clientBuilder = clientBuilder.UseGrpcEndpoint(_properties.DaprUri);
        }
        else if (_properties.DaprProtocol == DaprProtocol.Grpc)
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
        return _broadcaster;
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
        return new DaprBroadcasterFactory(properties);
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
    }}