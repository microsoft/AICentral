namespace AICentral.Endpoints.OpenAI;

public class OpenAIEndpointPropertiesConfig
{
    public Dictionary<string, string>? ModelMappings { get; init; }
    public Dictionary<string, string>? AssistantMappings { get; init; }
    public string? ApiKey { get; set; }
    public string? Organization { get; set; }
    public int? MaxConcurrency { get; set; }
}