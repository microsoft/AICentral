namespace AICentral.Dapr.Broadcast;

public class DaprBroadcastOptions
{
    public string DaprPubSubComponentName { get; set; }
    public string PubSubTopicName { get; set; }
    public DaprProtocol? DaprProtocol { get; set; }
    public string? DaprUri { get; set; }
    public string? DaprToken { get; set; }
}