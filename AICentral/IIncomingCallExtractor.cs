using AICentral.Core;

namespace AICentral;

/// <summary>
/// Extracts important information from the incoming call so we can translate between various provides such
/// as Open AI or Azure Open AI.
/// </summary>
public interface IIncomingCallExtractor
{
    Task<AICallInformation> Extract(HttpRequest request, CancellationToken cancellationToken);
}