namespace AICentral.Dapr.Audit;

public class AICentralDaprAuditOptions
{
    public string PubSubName { get; set; }
    public string TopicName { get; set; }
    public string StateStore { get; set; }
    public bool PIIStrippingDisabled { get; set; }
}
