using System.Text.Json;
using System.Text.Json.Nodes;
using AICentral.Core;

namespace AICentral.AzureAISearchVectorizer;

public class AdaptJsonToAzureAISearchTransformer: IResponseTransformer
{
    private readonly JsonNode _incomingDocument;

    public AdaptJsonToAzureAISearchTransformer(JsonNode incomingDocument)
    {
        _incomingDocument = incomingDocument;
    }
    public JsonDocument Transform(JsonDocument input)
    {
        var embeddingElement = input.RootElement.GetProperty("data")[0].GetProperty("embedding");
        var embeddings = embeddingElement.EnumerateArray().Select(x => (JsonNode)x.GetSingle());
        var dataProperty = _incomingDocument["values"]![0]!["data"]!;
        dataProperty["vector"] = new JsonArray(embeddings.ToArray());
        return _incomingDocument.Deserialize<JsonDocument>()!;
    }
}