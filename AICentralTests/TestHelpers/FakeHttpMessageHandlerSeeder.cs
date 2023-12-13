using Microsoft.Extensions.DependencyInjection;

namespace AICentralTests.TestHelpers;

public class FakeHttpMessageHandlerSeeder
{
    public Dictionary<string, Func<Task<HttpResponseMessage>>> SeededResponses { get; } = new();

    public void Seed(string url, Func<Task<HttpResponseMessage>> response)
    {
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url);
        SeededResponses.Add(url, response);
    }

    public void SeedChatCompletions(string endpoint, string modelName, Func<Task<HttpResponseMessage>> response, string apiVersion="2023-05-15")
    {
        var url = $"https://{endpoint}/openai/deployments/{modelName}/chat/completions?api-version={apiVersion}";
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url);
        SeededResponses.Add(url, response);
    }

    public void SeedCompletions(string endpoint, string modelName, Func<Task<HttpResponseMessage>> response)
    {
        var url = $"https://{endpoint}/openai/deployments/{modelName}/completions?api-version=2023-05-15";
        if (SeededResponses.ContainsKey(url)) SeededResponses.Remove(url);
        SeededResponses.Add(url, response);
    }
}

public static class TestWebApplicationFactoryEx
{
    public static void SeedChatCompletions(this TestWebApplicationFactory<Program> webApplicationFactory, string endpoint,
        string modelName,
        Func<Task<HttpResponseMessage>> response, 
        string apiVersion="2023-05-15")
    {
        webApplicationFactory.Services.GetRequiredService<FakeHttpMessageHandlerSeeder>()
            .SeedChatCompletions(endpoint, modelName, response, apiVersion);
    }

    public static void SeedCompletions(
        this TestWebApplicationFactory<Program> webApplicationFactory, 
        string endpoint,
        string modelName,
        Func<Task<HttpResponseMessage>> response)
    {
        webApplicationFactory.Services.GetRequiredService<FakeHttpMessageHandlerSeeder>()
            .SeedCompletions(endpoint, modelName, response);
    }

    public static void Seed(this TestWebApplicationFactory<Program> webApplicationFactory, string url, Func<Task<HttpResponseMessage>> response)
    {
        webApplicationFactory.Services.GetRequiredService<FakeHttpMessageHandlerSeeder>()
            .Seed(url, response);
    }

}