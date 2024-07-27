namespace AICentral;

/// <summary>
/// Configuration type representing an AI Central Pipeline
/// </summary>
public class PipelineConfig
{
    /// <summary>
    /// Name of the pipeline
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// Hostname to look for to dispatch requests to this pipeline
    /// </summary>
    public string? Host { get; init; }
    
    /// <summary>
    /// Name of a pre-configured Endpoint Selector to use
    /// </summary>
    public string? EndpointSelector { get; init; }
    
    /// <summary>
    /// Auth Provider to use for consumer-auth on the pipeline
    /// </summary>
    public string? AuthProvider { get; init; }
    
    /// <summary>
    /// Array of step-names to execute in order
    /// </summary>
    public string[]? Steps { get; init; }
    
    /// <summary>
    /// Array of route-proxies to provide additional endpoints
    /// </summary>
    public string[]? RouteProxies { get; init; }

    public OTelConfig? OpenTelemetryConfig
    {
        get;
        init;
    }
}