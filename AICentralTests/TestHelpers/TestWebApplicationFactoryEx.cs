using System.Text;
using AICentralWeb;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentralTests.TestHelpers;

public static class TestWebApplicationFactoryEx
{
    public static void SeedChatCompletions(
        this TestWebApplicationFactory<Program> webApplicationFactory,
        string endpoint,
        string modelName,
        Func<Task<HttpResponseMessage>> response,
        string apiVersion = "2023-05-15")
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

    public static void Seed(this TestWebApplicationFactory<Program> webApplicationFactory, string url,
        Func<Task<HttpResponseMessage>> response)
    {
        webApplicationFactory.Services.GetRequiredService<FakeHttpMessageHandlerSeeder>()
            .Seed(url, response);
    }

    public static JObject[] EndpointRequests(this TestWebApplicationFactory<Program> webApplicationFactory)
    {
        return webApplicationFactory
            .Services
            .GetRequiredService<FakeHttpMessageHandlerSeeder>()
            .IncomingRequests
            .Select(x =>
            {
                var streamBytes = x.Value;

                var contentInformation = x.Key.Content?.Headers.ContentType?.MediaType == "application/json" ||
                                         x.Key.Content?.Headers.ContentType?.MediaType == "text/plain"
                    ? (object)Encoding.UTF8.GetString(streamBytes)
                    : new
                    {
                        Type = x.Key.Content?.Headers.ContentType?.MediaType,
                        Length = streamBytes.Length
                    };

                return JObject.FromObject(new
                {
                    Uri = x.Key.RequestUri!.PathAndQuery,
                    Method = x.Key.Method.ToString(),
                    Headers = x.Key.Headers.Where(x => x.Key != "x-ms-client-request-id")
                        .ToDictionary(h => h.Key, h => string.Join(';', h.Value)),
                    Content = contentInformation,
                });
            }).ToArray();
    }

    public static Dictionary<string, object> VerifyRequestsAndResponses(
        this TestWebApplicationFactory<Program> webApplicationFactory,
        HttpResponseMessage response)
    {
        return new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(webApplicationFactory.EndpointRequests(), Formatting.Indented),
            ["Response"] = new
            {
                Headers = response.Headers,
                Content = JsonConvert.SerializeObject(JObject.Parse(response.Content.ReadAsStringAsync().Result),
                    Formatting.Indented)
            }
        };
    }

    public static Dictionary<string, object> VerifyRequestsAndResponses(
        this TestWebApplicationFactory<Program> webApplicationFactory,
        object response)
    {
        return new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(webApplicationFactory.EndpointRequests(), Formatting.Indented),
            ["Response"] = JsonConvert.SerializeObject(JObject.FromObject(response), Formatting.Indented)
        };
    }

    public static void Clear(this TestWebApplicationFactory<Program> webApplicationFactory)
    {
        webApplicationFactory.Services.GetRequiredService<FakeHttpMessageHandlerSeeder>().Clear();
    }
}