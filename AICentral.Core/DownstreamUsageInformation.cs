namespace AICentral.Core;

public record DownstreamUsageInformation(
    string OpenAIHost,
    string? ModelName,
    string Client,
    AICallType CallType,
    bool? StreamingResponse,
    string? Prompt,
    string? Response,
    Lazy<(int? EstimatedPromptTokens, int? EstimatedCompletionTokens)>? EstimatedTokens,
    (int PromptTokens, int CompletionTokens, int TotalTokens)? KnownTokens,
    string RemoteIpAddress,
    DateTimeOffset StartDate,
    TimeSpan Duration)
{
    
    public static DownstreamUsageInformation Empty(
        HttpContext context, 
        IncomingCallDetails incomingCallDetails,
        string hostUriBase)
        =>
            new DownstreamUsageInformation(
                hostUriBase,
                string.Empty,
                context.User.Identity?.Name ?? string.Empty,
                incomingCallDetails.AICallType,
                null,
                incomingCallDetails.PromptText,
                null,
                null,
                null,
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                context.RequestServices.GetRequiredService<IDateTimeProvider>().Now, TimeSpan.Zero);
    
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