namespace AICentral.Core;

public class AICentralPipelineConfig
{
    public string? Name { get; init; }
    public string? Host { get; init; }
    public string? EndpointSelector { get; init; }
    public string? AuthProvider { get; init; }
    public string[]? Steps { get; init; }
}