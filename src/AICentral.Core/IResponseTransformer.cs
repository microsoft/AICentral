using System.Text.Json;

namespace AICentral.Core;

public interface IResponseTransformer
{
    JsonDocument Adapt(JsonDocument input);
}