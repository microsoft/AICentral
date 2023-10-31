namespace AICentral.Configuration.JSON;

public static class ConfigurationTypes
{
    public class AICentralConfig
    {
        public AICentralGenericStepConfig[]? GenericSteps { get; init; }
        public AICentralAuthConfig[]? AuthProviders { get; init; }
        public AICentralPipelineEndpointConfig[]? Endpoints { get; init; }
        public AICentralPipelineEndpointSelectorConfig[]? EndpointSelectors { get; init; }
        public AICentralPipelineConfig[]? Pipelines { get; init; }
        public bool ExposeTestPage { get; set; }
    };

    public class AICentralAuthConfig
    {
        public string? Type { get; init; }
        public string? Name { get; init; }
        public Dictionary<string, string>? Properties { get; init; }
    }

    public class AICentralGenericStepConfig
    {
        public string? Type { get; init; }
        public string? Name { get; init; }
        public Dictionary<string, string>? Properties { get; init; }
    }

    public class AICentralPipelineEndpointConfig
    {
        public string? Type { get; init; }
        public string? Name { get; init; }
        public Dictionary<string, string>? Properties { get; init; }
    }

    public class AICentralPipelineEndpointSelectorConfig
    {
        public string? Type { get; init; }
        public string? Name { get; init; }
        public Dictionary<string, string>? Properties { get; init; }
    }


    public class AICentralPipelineConfig
    {
        public string? Name { get; init; }
        public AICentralComponentConfig? Path { get; init; }
        public string? EndpointSelector { get; init; }
        public string? AuthProvider { get; set; }
        public string? RateLimiter { get; set; }
        public string[]? Steps { get; init; }
    }

    public class AICentralComponentConfig
    {
        public string? Type { get; init; }
        public Dictionary<string, string>? Properties { get; init; }
    }
}