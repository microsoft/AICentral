using System.Collections.Concurrent;

namespace AICentralTests.TestHelpers;

public class FakeHttpMessageHandlerSeeder
{
    private ConcurrentDictionary<string, Func<HttpRequestMessage, Task<HttpResponseMessage>>> SeededResponses { get; } = new();
    public List<(HttpRequestMessage, byte[])> IncomingRequests { get; } = new();

    public async Task<HttpResponseMessage?> TryGet(HttpRequestMessage request)
    {
        if (SeededResponses.TryGetValue(request.RequestUri!.AbsoluteUri, out var responseFunction))
        {
            var response = await responseFunction(request);
            if (response.IsSuccessStatusCode)
            {
                IncomingRequests.Add((request, request.Content?.ReadAsByteArrayAsync().Result ?? Array.Empty<byte>()));
            }

            return response;
        }
        return null;
    }

    public void Seed(string url, Func<HttpRequestMessage, Task<HttpResponseMessage>> response)
    {
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url, out _);
        SeededResponses.TryAdd(url, response);
    }

    public void SeedChatCompletions(string endpoint, string modelName, Func<Task<HttpResponseMessage>> response,
        string apiVersion = "2023-05-15")
    {
        var url = $"https://{endpoint}/openai/deployments/{modelName}/chat/completions?api-version={apiVersion}";
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url, out _);
        SeededResponses.TryAdd(url, _ => response());
    }

    public void SeedCompletions(string endpoint, string modelName, Func<Task<HttpResponseMessage>> response)
    {
        var url = $"https://{endpoint}/openai/deployments/{modelName}/completions?api-version=2023-05-15";
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url, out _);
        SeededResponses.TryAdd(url, _ => response());
    }

    public void Clear()
    {
        SeededResponses.Clear();
        IncomingRequests.Clear();
    }
}