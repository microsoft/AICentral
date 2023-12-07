using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace AICentralTests.TestHelpers;

public class AICentralFakeResponses
{
    public static readonly string Endpoint500 = Guid.NewGuid().ToString();
    public static readonly string Endpoint404 = Guid.NewGuid().ToString();
    public static readonly string Endpoint200 = Guid.NewGuid().ToString();
    public static readonly string Endpoint200Number2 = Guid.NewGuid().ToString();
    public static readonly string FastEndpoint = Guid.NewGuid().ToString();
    public static readonly string SlowEndpoint = Guid.NewGuid().ToString();
    public static readonly string FakeResponseId = "chatcmpl-6v7mkQj980V1yBec6ETrKPRqFjNw9";

    public static HttpResponseMessage FakeChatCompletionsResponse(int? totalTokens = 126)
    {
        var response = new HttpResponseMessage();
        response.Content = new StringContent(
            JsonConvert.SerializeObject(new
            {
                id = FakeResponseId,
                @object = "chat.completion",
                created = 1679072642,
                model = "gpt-35-turbo",
                usage = new
                {
                    prompt_tokens = 58,
                    completion_tokens = 68,
                    total_tokens = totalTokens
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

        return response;
    }

    public static HttpResponseMessage FakeCompletionsResponse()
    {
        var response = new HttpResponseMessage();
        response.Content = new StringContent(
            JsonConvert.SerializeObject(new
            {
                id = FakeResponseId,
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
                        text =
                            "Yes, other Azure AI services also support customer managed keys. Azure AI services offer multiple options for customers to manage keys, such as using Azure Key Vault, customer-managed keys in Azure Key Vault or customer-managed keys through Azure Storage service. This helps customers ensure that their data is secure and access to their services is controlled.",
                        finish_reason = "stop",
                        index = 0
                    }
                },
            })
            , Encoding.UTF8, "application/json");

        return response;
    }

    public static HttpResponseMessage FakeAzureOpenAIImageResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted);
        response.Headers.Add("operation-location",
            $"https://{Endpoint200}/openai/operations/images/f508bcf2-e651-4b4b-85a7-58ad77981ffa?api-version=2023-09-01-preview");
        response.Content = new StringContent(
            JsonConvert.SerializeObject(new
            {
                id = "f508bcf2-e651-4b4b-85a7-58ad77981ffa",
                status = "notRunning",
            })
            , Encoding.UTF8, "application/json");

        return response;
    }

    public static HttpResponseMessage FakeAzureOpenAIImageStatusResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        response.Content = new StringContent(
            JsonConvert.SerializeObject(new
            {
                id = "f508bcf2-e651-4b4b-85a7-58ad77981ffa",
                created = created,
                status = "succeeded",
                result = new
                {
                    created = created,
                    data = new[]
                    {
                        new
                        {
                            url = "https://images.localtest.me/some-image-somehere"
                        }
                    }
                }
            })
            , Encoding.UTF8, "application/json");

        return response;
    }

    public static HttpResponseMessage FakeOpenAIAudioTranscriptionResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("""
                                             1
                                             00:00:00,000 --> 00:00:07,000
                                             I wonder what the translation will be for this
                                             """, Encoding.UTF8, "text/plain");

        response.Headers.Add("openai-processing-ms", "744");
        response.Headers.Add("openai-version", "2020-10-01");
        response.Headers.Add("x-ratelimit-limit-requests", "50");
        response.Headers.Add("x-ratelimit-remaining-requests", "49");
        response.Headers.Add("x-ratelimit-reset-requests", "1.2s");
        return response;
    }

    public static HttpResponseMessage NotFoundResponse()
    {
        var response = new HttpResponseMessage();
        response.StatusCode = HttpStatusCode.NotFound;
        return response;
    }

    public static HttpResponseMessage InternalServerErrorResponse()
    {
        var response = new HttpResponseMessage();
        response.StatusCode = HttpStatusCode.InternalServerError;
        return response;
    }
}