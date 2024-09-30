namespace AICentral.Dapr.Broadcast;

public class DaprBroadcastOptions
{
    public string DaprUri { get; set; }
    public string? DaprToken { get; set; }
    public string DaprPubSubComponentName { get; set; }
    public string PubSubTopicName { get; set; }
    public DaprProtocol DaprProtocol { get; set; }
}