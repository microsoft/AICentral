using System.Text.Json.Nodes;

namespace AICentral.Core;

/// <summary>
/// Represents the request to the Downstream AI Service before metadata is extracted
/// </summary>
/// <param name="LanguageUrl">Url called</param>
/// <param name="CallType">Type of AI Call</param>
/// <param name="ResponseType">Type of Request (e.g. will it be a Server Side Event streaming response)</param>
/// <param name="DeploymentName">Which Deployment (or Model) was called</param>
/// <param name="Prompt">The prompt passed by the consumer</param>
/// <param name="StartDate">When the call to the downstream was made</param>
/// <param name="Duration">Duration of the downstream call</param>
/// <param name="RawRequest">Raw JSON content of request (if JSON request)</param>
public record DownstreamRequestInformation(string LanguageUrl, string InternalEndpointName, AICallType CallType, AICallResponseType ResponseType, string? DeploymentName, string? Prompt, DateTimeOffset StartDate, TimeSpan Duration, JsonNode? RawRequest);
