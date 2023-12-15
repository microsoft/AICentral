namespace AICentral.Core;

public record DownstreamUsageInformation(
    string OpenAIHost,
    string? ModelName,
    string Client,
    AICallType CallType,
    string? Prompt,
    string? Response,
    int? EstimatedPromptTokens,
    int? EstimatedCompletionTokens,
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens,
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
                context.User.Identity?.Name ?? "unknown",
                incomingCallDetails.AICallType,
                incomingCallDetails.PromptText,
                null,
                null,
                null,
                null,
                null,
                null,
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                context.RequestServices.GetRequiredService<IDateTimeProvider>().Now, TimeSpan.Zero);
}