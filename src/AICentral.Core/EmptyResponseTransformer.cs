using System.Text.Json;

namespace AICentral.Core;

public class EmptyResponseTransformer : IResponseTransformer
{
    public JsonDocument Transform(JsonDocument input)
    {
        return input;
    }
}