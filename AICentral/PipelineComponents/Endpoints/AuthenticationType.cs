namespace AICentral.PipelineComponents.Endpoints;

public enum AuthenticationType
{
    /// <summary>
    /// Switch to an api-key
    /// </summary>
    ApiKey,
    /// <summary>
    /// Assume a managed identity (obtained using DefaultAzureCredential)
    /// </summary>
    Entra,
    /// <summary>
    /// Pass through an incoming bearer token
    /// </summary>
    EntraPassThrough
}