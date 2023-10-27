using System.Text;
using AICentral.Pipelines.Endpoints;
using AICentral.Pipelines.Endpoints.EndpointAuth;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Polly;

namespace AICentralTests;

public class FakeEndpointDispatcher : IAIEndpointDispatcher
{
    public Task<HttpResponseMessage> Dispatch(HttpClient httpClient, HttpContext context,
        ResiliencePipeline<HttpResponseMessage> retry, string endpointUrl, string requestRawContent,
        IEndpointAuthorisationHandler authHandler, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage();
        response.Headers.Add("x-test", "test");
        response.Content = new StringContent(
            JsonConvert.SerializeObject(new
            {
                id = "chatcmpl-6v7mkQj980V1yBec6ETrKPRqFjNw9", 
                @object = "chat.completion", 
                created = 1679072642,
                model = "gpt-35-turbo",
                usage = new
                {
                    prompt_tokens = 58,
                    completion_tokens = 68,
                    total_tokens = 126
                },
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            role = "assistant",
                            content =
                                "Yes, other Azure AI services also support customer managed keys. Azure AI services offer multiple options for customers to manage keys, such as using Azure Key Vault, customer-managed keys in Azure Key Vault or customer-managed keys through Azure Storage service. This helps customers ensure that their data is secure and access to their services is controlled."
                        },
                        finish_reason = "stop", 
                        index = 0
                    }
                },
            })
            , Encoding.UTF8, "application/json");
        return Task.FromResult(response);
    }
}