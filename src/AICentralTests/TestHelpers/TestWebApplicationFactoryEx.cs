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
        string apiVersion = "2024-02-15-preview")
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
            .Seed(url, req => response());
    }

    public static void Seed(this TestWebApplicationFactory<Program> webApplicationFactory, string url,
        Func<HttpRequestMessage, Task<HttpResponseMessage>> response)
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
                var streamBytes = x.Item2;

                var contentInformation = x.Item1.Content?.Headers.ContentType?.MediaType == "application/json" ||
                                         x.Item1.Content?.Headers.ContentType?.MediaType == "text/plain"
                    ? (object)Encoding.UTF8.GetString(streamBytes)
                    : new
                    {
                        Type = x.Item1.Content?.Headers.ContentType?.MediaType,
                        Length = streamBytes.Length
                    };

                return JObject.FromObject(new
                {
                    Uri = x.Item1.RequestUri!.PathAndQuery,
                    Method = x.Item1.Method.ToString(),
                    Headers = x.Item1.Headers.Where
                            (x => x.Key != "x-ms-client-request-id" && x.Key != "User-Agent" && x.Key != "Authorization" && x.Key != "OpenAI-Organization")
                        .ToDictionary(h => h.Key, h => string.Join(';', h.Value)),
                    ContentType = x.Item1.Content?.Headers.ContentType?.MediaType,
                    Content = contentInformation,
                });
            }).ToArray();
    }

    public static JObject[] GetDiagnostics(this TestWebApplicationFactory<Program> webApplicationFactory)
    {
        return webApplicationFactory
            .Services
            .GetRequiredService<FakeHttpMessageHandlerSeeder>()
            .IncomingRequests
            .Select(x =>
            {
                var streamBytes = x.Item2;

                var contentInformation = x.Item1.Content?.Headers.ContentType?.MediaType == "application/json" ||
                                         x.Item1.Content?.Headers.ContentType?.MediaType == "text/plain"
                    ? (object)Encoding.UTF8.GetString(streamBytes)
                    : new
                    {
                        Type = x.Item1.Content?.Headers.ContentType?.MediaType,
                        Length = streamBytes.Length
                    };

                return JObject.FromObject(new
                {
                    Uri = x.Item1.RequestUri!.PathAndQuery,
                    Method = x.Item1.Method.ToString(),
                    Headers = x.Item1.Headers.Where(x => x.Key != "x-ms-client-request-id" && x.Key != "User-Agent")
                        .ToDictionary(h => h.Key, h => string.Join(';', h.Value)),
                    ContentType = x.Item1.Content?.Headers.ContentType?.MediaType,
                    Content = contentInformation,
                });
            }).ToArray();
    }

    public static Dictionary<string, object> VerifyRequestsAndResponses(
        this TestWebApplicationFactory<Program> webApplicationFactory,
        HttpResponseMessage response, bool validateResponseMetadata = false)
    {
        var validation = new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(webApplicationFactory.EndpointRequests(), Formatting.Indented),
            ["Response"] = new
            {
                Headers = response.Headers.Where(x => !x.Key.StartsWith("x-ai")),
                Content = JsonConvert.SerializeObject(JObject.Parse(response.Content.ReadAsStringAsync().Result),
                    Formatting.Indented)
            }
        };

        if (validateResponseMetadata)
        {
            response.Headers.TryGetValues("x-aicentral-test-diagnostics", out var key); 
            var downstreamUsageInformation = webApplicationFactory.Services.GetRequiredService<DiagnosticsCollector>().DownstreamUsageInformation[key!.Single()];
            var info = downstreamUsageInformation with { Duration = TimeSpan.Zero, EstimatedTokens = null};
            validation["ResponseMetadata"] =  info;
        }
        
        return validation;
    }

    public static Dictionary<string, object> VerifyRequestsAndResponses(
        this TestWebApplicationFactory<Program> webApplicationFactory,
        Azure.Response response, bool validateResponseMetadata = false)
    {
        var validation = new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(webApplicationFactory.EndpointRequests(), Formatting.Indented),
            ["Response"] = new
            {
                Headers = response.Headers.Where(x => !x.Name.StartsWith("x-ai"))
            }
        };

        if (validateResponseMetadata)
        {
            response.Headers.TryGetValues("x-aicentral-test-diagnostics", out var key); 
            var downstreamUsageInformation = webApplicationFactory.Services.GetRequiredService<DiagnosticsCollector>().DownstreamUsageInformation[key!.Single()];
            var info = downstreamUsageInformation with { Duration = TimeSpan.Zero, EstimatedTokens = null};
            validation["ResponseMetadata"] =  info;
        }
        
        return validation;
    }

    public static Dictionary<string, object> VerifyRequestsAndResponses<T>(
        this TestWebApplicationFactory<Program> webApplicationFactory,
        Azure.Response<T> response, bool validateResponseMetadata = false)
    {
        var validation = new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(webApplicationFactory.EndpointRequests(), Formatting.Indented),
            ["Response"] = JsonConvert.SerializeObject(response, Formatting.Indented)
        };
        if (validateResponseMetadata)
        {
            response.GetRawResponse().Headers.TryGetValues("x-aicentral-test-diagnostics", out var key); 
            var downstreamUsageInformation = webApplicationFactory.Services.GetRequiredService<DiagnosticsCollector>().DownstreamUsageInformation[key!.Single()];
            var info = downstreamUsageInformation with { Duration = TimeSpan.Zero, EstimatedTokens = null};
            validation["ResponseMetadata"] =  info;
        }

        return validation;

    }

    public static Dictionary<string, object> VerifyRequestsAndResponses(
        this TestWebApplicationFactory<Program> webApplicationFactory,
        object response)
    {
        var validation = new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(webApplicationFactory.EndpointRequests(), Formatting.Indented),
            ["Response"] = JsonConvert.SerializeObject(response, Formatting.Indented)
        };
        return validation;

    }

    public static void Clear(this TestWebApplicationFactory<Program> webApplicationFactory)
    {
        webApplicationFactory.Services.GetRequiredService<FakeHttpMessageHandlerSeeder>().Clear();
    }
}