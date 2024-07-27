using System.Text.Json;

namespace AICentral.Core;

public interface ITransformIncomingJsonDocumentsToOpenAIJsonDocuments
{
    JsonDocument Adapt(JsonDocument input);
}