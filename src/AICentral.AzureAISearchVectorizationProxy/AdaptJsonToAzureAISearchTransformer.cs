using System.Text.Json;
using System.Text.Json.Nodes;
using AICentral.Core;
using AICentral.ResultHandlers;

namespace AICentral.AzureAISearchVectorizationProxy;

public class AdaptJsonToAzureAISearchTransformer: ITransformIncomingJsonDocumentsToOpenAIJsonDocuments
{
    private readonly JsonNode _incomingDocument;

    public AdaptJsonToAzureAISearchTransformer(JsonNode incomingDocument)
    {
        _incomingDocument = incomingDocument;
    }
    public JsonDocument Adapt(JsonDocument input)
    {
        var embeddingElement = input.RootElement.GetProperty("data")[0].GetProperty("embedding");
        var embeddings = embeddingElement.EnumerateArray().Select(x => (JsonNode)x.GetSingle());
        var dataProperty = _incomingDocument["values"]![0]!["data"]!;
        dataProperty["vector"] = new JsonArray(embeddings.ToArray());
        return _incomingDocument.Deserialize<JsonDocument>()!;
    }
}