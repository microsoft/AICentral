using System.Net;
using System.Text;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.OpenAILike.AzureOpenAI;
using Newtonsoft.Json;

namespace AICentralTests.TestHelpers;

public class AICentralFakeResponses
{
    public static readonly string Endpoint500 = Guid.NewGuid().ToString();
    public static readonly string Endpoint404 = Guid.NewGuid().ToString();
    public static readonly string Endpoint200 = Guid.NewGuid().ToString();
    public static readonly string Endpoint200Number2 = Guid.NewGuid().ToString();
    public static readonly string Endpoint200Image = Guid.NewGuid().ToString();

    public static HttpResponseMessage FakeImageResponse()
    {
        return new HttpResponseMessage()
        {
            Content = new StringContent(
                JsonConvert.SerializeObject(new
                {
                    Created = new DateTimeOffset(2023, 11, 7, 11, 10, 0, TimeSpan.FromHours(8)).ToUnixTimeSeconds(),
                    Data = new []
                    {
                        new
                        {
                            url = "https://fakeimage.localtest.me/some-image"
                        }
                    }
                }))
        };
    }

    public static HttpResponseMessage FakeResponse()
    {
        var response = new HttpResponseMessage();
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