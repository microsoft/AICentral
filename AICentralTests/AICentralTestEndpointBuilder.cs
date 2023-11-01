using AICentral.PipelineComponents.Endpoints.AzureOpenAI;
using AICentral.PipelineComponents.Endpoints.EndpointAuth;
using Polly;

namespace AICentralTests;

public class AICentralTestEndpointBuilder
{
    public static AzureOpenAIEndpointDispatcher Random() =>
        new(
            $"https://{Guid.NewGuid().ToString()}",
            new Dictionary<string, string>(),
            new KeyAuth("test"),
            ResiliencePipeline<HttpResponseMessage>.Empty);
}