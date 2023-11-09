using AICentral.Steps.Endpoints;

namespace AICentral.Configuration.JSON;

public static class ConfigurationTypes
{
    public class AICentralConfig
    {
        public AICentralPipelineConfig[]? Pipelines { get; init; }
    }
    
    public class AICentralTypeAndNameConfig
    {
        public string? Type { get; init; }
        public string? Name { get; init; }
    }

    public class AICentralPipelineAzureOpenAIEndpointPropertiesConfig
    {
        public string? LanguageEndpoint { get; init; }
        public Dictionary<string, string>? ModelMappings { get; init; }
        public AuthenticationType? AuthenticationType { get; init; }
        public string? ApiKey { get; set; }
    }

    public class AICentralPipelineOpenAIEndpointPropertiesConfig
    {
        public Dictionary<string, string>? ModelMappings { get; init; }
        public string? ApiKey { get; set; }
        public string? Organization { get; set; }
    }

    public class AICentralPipelineConfig
    {
        public string? Name { get; init; }
        public string? Host { get; init; }
        public string? EndpointSelector { get; init; }
        public string? AuthProvider { get; init; }
        public string[]? Steps { get; init; }
    }

    public class ApiKeyClientAuthConfig
    {
        public ApiKeyClientAuthClientConfig[]? Clients { get; init; }
    }

    public class ApiKeyClientAuthClientConfig
    {
        public string? ClientName { get; init; }
        public string? Key1 { get; init; }
        public string? Key2 { get; init; }
    }

    public class PriorityEndpointConfig
    {
        public string[]? PriorityEndpoints { get; init; }
        public string[]? FallbackEndpoints { get; init; }
    }

    public class RandomEndpointConfig
    {
        public string[]? Endpoints { get; init; }
    }

    public class AzureMonitorLoggingConfig
    {
        public string? WorkspaceId { get; init; }
        public string? Key { get; init; }
        public bool? LogPrompt { get; init; }
        public bool? LogResponse { get; init; }
    }
}