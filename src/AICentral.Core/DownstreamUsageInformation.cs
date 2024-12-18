﻿using System.Text.Json;
using System.Text.Json.Nodes;

namespace AICentral.Core;

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
    bool? Success,
    string? RawPrompt)
{
    
    public static DownstreamUsageInformation Empty(
        IRequestContext context, 
        IncomingCallDetails incomingCallDetails,
        string? hostUriBase,
        string? internalEndpointName)
        =>
            new DownstreamUsageInformation(
                internalEndpointName,
                hostUriBase,
                string.Empty,
                string.Empty,
                context.UserName ?? string.Empty,
                incomingCallDetails.AICallType,
                null,
                incomingCallDetails.PromptText,
                null,
                null,
                null,
                null,
                context.RemoteIpAddress,
                context.Now, 
                TimeSpan.Zero, 
                null,
                incomingCallDetails.RequestContent?.ToJsonString());
    
    public int? PromptTokens => KnownTokens?.PromptTokens ?? EstimatedTokens?.Value.EstimatedPromptTokens;
    public int? CompletionTokens => KnownTokens?.CompletionTokens ?? EstimatedTokens?.Value.EstimatedCompletionTokens;
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