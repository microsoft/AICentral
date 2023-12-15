using Microsoft.Extensions.Primitives;

namespace AICentral.Core;

/// <summary>
/// Represents a pre-process of the raw response. SanitisedHeaders are headers that will get passed back to the client. Remaining Tokens and Requests will be emitted as Telemetry.  
/// </summary>
/// <param name="SanitisedHeaders"></param>
/// <param name="RemainingTokens"></param>
/// <param name="RemainingRequests"></param>
public record ResponseMetadata(Dictionary<string, StringValues> SanitisedHeaders, bool RequiresAffinityOnSubsequentRequests, long? RemainingTokens, long? RemainingRequests);
