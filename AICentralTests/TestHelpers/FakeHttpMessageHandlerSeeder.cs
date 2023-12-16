using System.Collections.Concurrent;

namespace AICentralTests.TestHelpers;

public class FakeHttpMessageHandlerSeeder
{
    private ConcurrentDictionary<string, Func<Task<HttpResponseMessage>>> SeededResponses { get; } = new();
    public ConcurrentDictionary<HttpRequestMessage, byte[]> IncomingRequests { get; } = new();

    public bool TryGet(HttpRequestMessage request, out Func<Task<HttpResponseMessage>>? response)
    {
        if (SeededResponses.TryGetValue(request.RequestUri!.AbsoluteUri, out var responseFunction))
        {
            response = responseFunction;
            IncomingRequests.TryAdd(request, request.Content!.ReadAsByteArrayAsync().Result);
            return true;
        }

        IncomingRequests.TryAdd(request, Array.Empty<byte>());
        response = null;
        return false;
    }

    public void Seed(string url, Func<Task<HttpResponseMessage>> response)
    {
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url, out _);
        SeededResponses.TryAdd(url, response);
    }

    public void SeedChatCompletions(string endpoint, string modelName, Func<Task<HttpResponseMessage>> response,
        string apiVersion = "2023-05-15")
    {
        var url = $"https://{endpoint}/openai/deployments/{modelName}/chat/completions?api-version={apiVersion}";
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url, out _);
        SeededResponses.TryAdd(url, response);
    }

    public void SeedCompletions(string endpoint, string modelName, Func<Task<HttpResponseMessage>> response)
    {
        var url = $"https://{endpoint}/openai/deployments/{modelName}/completions?api-version=2023-05-15";
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url, out _);
        SeededResponses.TryAdd(url, response);
    }

    public void Clear()
    {
        SeededResponses.Clear();
        IncomingRequests.Clear();
    }
}