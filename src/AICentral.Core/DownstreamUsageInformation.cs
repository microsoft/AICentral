﻿namespace AICentral.Core;

/// <summary>
/// Represents information about the Downstream AI Service usage information
/// </summary>
public record DownstreamUsageInformation(
    string? InternalEndpointName,
    string? OpenAIHost,
    string? ModelName,
    string? DeploymentName,
    string Client,
    AICallType CallType,
    bool? StreamingResponse,
    string? Prompt,
    string? Response,
    Lazy<(int? EstimatedPromptTokens, int? EstimatedCompletionTokens)>? EstimatedTokens,
    (int PromptTokens, int CompletionTokens, int TotalTokens)? KnownTokens,
    ResponseMetadata? ResponseMetadata,
    string RemoteIpAddress,
    DateTimeOffset StartDate,
    TimeSpan Duration,
    bool? Success)
{
    
    public static DownstreamUsageInformation Empty(
        HttpContext context, 
        IncomingCallDetails incomingCallDetails,
        ResponseMetadata? responseMetadata,
        string? hostUriBase,
        string? internalEndpointName)
        =>
            new DownstreamUsageInformation(
                internalEndpointName,
                hostUriBase,
                string.Empty,
                string.Empty,
                context.User.Identity?.Name ?? string.Empty,
                incomingCallDetails.AICallType,
                null,
                incomingCallDetails.PromptText,
                null,
                null,
                null,
                null,
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                context.RequestServices.GetRequiredService<IDateTimeProvider>().Now, 
                TimeSpan.Zero, 
                null);
    
    public int? TotalTokens
    {
        get
        {
            if (KnownTokens != null) return KnownTokens.Value.TotalTokens;
            
            if (EstimatedTokens != null)
                return EstimatedTokens.Value.EstimatedPromptTokens + EstimatedTokens.Value.EstimatedCompletionTokens;

            return null;
        }
    }
}