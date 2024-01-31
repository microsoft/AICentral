namespace AICentral.Core;

/// <summary>
/// Well-known header names used by AI Central. TODO - rename to AICentralQueryPartNames.
/// </summary>
public static class AICentralHeaders
{
    /// <summary>
    /// The name used in the query string to identify which downstream endpoint to call for asynchronous requests like DALLE-2 on Azure Open AI.
    /// </summary>
    public const string AzureOpenAIHostAffinityHeader = "ai-central-host-affinity";
}