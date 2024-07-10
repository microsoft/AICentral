using System.Text.Json.Nodes;

namespace AICentral;

public static class JsonNodeEx
{
    public static bool TryGetProperty(this JsonNode node, string property, out JsonNode nodeOut)
    {
        if (node[property] != null)
        {
            nodeOut = node[property]!;
            return true;
        }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        nodeOut = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        return false;
    }
    
}