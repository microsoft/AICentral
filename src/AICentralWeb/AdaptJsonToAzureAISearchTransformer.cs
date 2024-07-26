using System.Text.Json;
using AICentral.ResultHandlers;

namespace AICentralWeb;

public class AdaptJsonToAzureAISearchTransformer: IAdaptJsonDocuments
{
    public JsonDocument Adapt(JsonDocument input)
    {
        return input;
    }
}