using System.Text.Json;

namespace AICentral.ResultHandlers;

public interface ITransformIncomingJsonDocumentsToOpenAIJsonDocuments
{
    JsonDocument Adapt(JsonDocument input);
}