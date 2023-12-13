namespace AICentral.OpenAI.OpenAI;

public class AICentralPipelineOpenAIEndpointPropertiesConfig
{
    public Dictionary<string, string>? ModelMappings { get; init; }
    public string? ApiKey { get; set; }
    public string? Organization { get; set; }
    public int? MaxConcurrency { get; set; }
}