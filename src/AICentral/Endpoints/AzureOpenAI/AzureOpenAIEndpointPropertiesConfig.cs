namespace AICentral.Endpoints.AzureOpenAI;

public class AzureOpenAIEndpointPropertiesConfig
{
    public string? LanguageEndpoint { get; init; }
    public string? AuthenticationType { get; init; }
    public string? ApiKey { get; set; }
    public Dictionary<string, string>? ModelMappings { get; init; }
    public Dictionary<string, string>? AssistantMappings { get; set; }
    public int? MaxConcurrency { get; set; }
}