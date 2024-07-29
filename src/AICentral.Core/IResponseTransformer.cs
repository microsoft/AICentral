using System.Text.Json;

namespace AICentral.Core;

/// <summary>
/// Allows a route-proxy to transform a JSON response from Azure Open AI into the format
/// required for the service consuming the proxy.
/// </summary>
public interface IResponseTransformer
{
    /// <summary>
    /// Transform the output from Open AI into that needed by the consuming proxy
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    JsonDocument Transform(JsonDocument input);
}