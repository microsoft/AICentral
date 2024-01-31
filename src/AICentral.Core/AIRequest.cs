namespace AICentral.Core;

/// <summary>
/// Represents a request to be sent to the AI service. ModelName does not need to be set if you don't have one.
/// </summary>
/// <param name="HttpRequestMessage">Raw Request message</param>
/// <param name="ModelName"></param>
public record AIRequest(HttpRequestMessage HttpRequestMessage, string? ModelName);