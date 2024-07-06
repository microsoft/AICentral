namespace AICentral.Endpoints.AzureOpenAI.Authorisers.BearerPassThroughWithAdditionalKey;

public class BearerPassThroughWithAdditionalKeyAuthFactoryConfig
{
    /// <summary>
    /// Claim to use from the incoming identity to find the additional key
    /// </summary>
    public string IncomingClaimName { get; init; } = default!;

    /// <summary>
    /// Name of the header to attach to downstream requests
    /// </summary>
    public string KeyHeaderName { get; init; } = default!;

    /// <summary>
    /// Mapping of incoming claims to outgoing keys
    /// </summary>
    public Dictionary<string, string> SubjectToKeyMappings { get; init; } = default!;
}
