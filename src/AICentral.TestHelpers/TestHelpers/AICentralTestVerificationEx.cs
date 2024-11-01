using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAIMock;

namespace AICentralTests.TestHelpers;

public static class AICentralTestVerificationEx
{
    
    public static Dictionary<string, object> VerifyRequestsAndResponses(
        this IServiceProvider services,
        HttpResponseMessage response, bool validateResponseMetadata = false)
    {
        var validation = new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(services.EndpointRequests(), Formatting.Indented),
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
            var downstreamUsageInformation = services.GetRequiredService<DiagnosticsCollector>().DownstreamUsageInformation[key!.Single()];
            var info = downstreamUsageInformation with { Duration = TimeSpan.Zero, EstimatedTokens = null};
            validation["ResponseMetadata"] =  info;
        }
        
        return validation;
    }
    
    

    public static Dictionary<string, object> VerifyRequestsAndResponsesStreaming(
        this IServiceProvider services,
        HttpResponseMessage response, bool validateResponseMetadata = false)
    {
        var validation = new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(services.EndpointRequests(), Formatting.Indented),
            ["Response"] = new
            {
                Headers = response.Headers.Where(x => !x.Key.StartsWith("x-ai")),
                Content = response.Content.ReadAsStringAsync().Result
            }
        };

        if (validateResponseMetadata)
        {
            response.Headers.TryGetValues("x-aicentral-test-diagnostics", out var key); 
            var downstreamUsageInformation = services.GetRequiredService<DiagnosticsCollector>().DownstreamUsageInformation[key!.Single()];
            var info = downstreamUsageInformation with { Duration = TimeSpan.Zero};
            validation["ResponseMetadata"] =  info;
        }
        
        return validation;
    }

    
    public static Dictionary<string, object> VerifyRequestsAndResponses(
        this IServiceProvider services,
        Azure.Response response, 
        bool validateResponseMetadata = false)
    {
        var endpointRequests = services.EndpointRequests();
        
        var validation = new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(endpointRequests, Formatting.Indented),
            ["Response"] = new
            {
                Headers = response.Headers.Where(x => !x.Name.StartsWith("x-ai"))
            }
        };

        if (validateResponseMetadata)
        {
            response.Headers.TryGetValues("x-aicentral-test-diagnostics", out var key); 
            var downstreamUsageInformation = services.GetRequiredService<DiagnosticsCollector>().DownstreamUsageInformation[key!.Single()];
            var info = downstreamUsageInformation with { Duration = TimeSpan.Zero, EstimatedTokens = null};
            validation["ResponseMetadata"] =  info;
        }
        
        return validation;
    }

    public static Dictionary<string, object> VerifyRequestsAndResponses<T>(
        this IServiceProvider services,
        Azure.Response<T> response, bool validateResponseMetadata = false)
    {
        var validation = new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(services.EndpointRequests(), Formatting.Indented),
            ["Response"] = JsonConvert.SerializeObject(response, Formatting.Indented)
        };
        if (validateResponseMetadata)
        {
            response.GetRawResponse().Headers.TryGetValues("x-aicentral-test-diagnostics", out var key); 
            var downstreamUsageInformation = services.GetRequiredService<DiagnosticsCollector>().DownstreamUsageInformation[key!.Single()];
            var info = downstreamUsageInformation with { Duration = TimeSpan.Zero, EstimatedTokens = null};
            validation["ResponseMetadata"] =  info;
        }

        return validation;

    }

}