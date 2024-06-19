using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenAIMock;

public static class OpenAIFakeResponses
{
    public static readonly string FakeResponseId = "chatcmpl-6v7mkQj980V1yBec6ETrKPRqFjNw9";

    public static void SeedChatCompletions(
        this IServiceProvider services,
        string endpoint,
        string modelName,
        Func<Task<HttpResponseMessage>> response,
        string apiVersion = "2024-04-01-preview")
    {
        services.GetRequiredService<FakeHttpMessageHandlerSeeder>()
            .SeedChatCompletions(endpoint, modelName, response, apiVersion);
    }

    public static void SeedCompletions(
        this IServiceProvider services,
        string endpoint,
        string modelName,
        Func<Task<HttpResponseMessage>> response)
    {
        services.GetRequiredService<FakeHttpMessageHandlerSeeder>()
            .SeedCompletions(endpoint, modelName, response);
    }

    public static void Seed(this IServiceProvider services, string url,
        Func<Task<HttpResponseMessage>> response)
    {
        services.GetRequiredService<FakeHttpMessageHandlerSeeder>()
            .Seed(url, _ => response());
    }

    public static void Seed(this IServiceProvider services, string url,
        Func<HttpRequestMessage, Task<HttpResponseMessage>> response)
    {
        services.GetRequiredService<FakeHttpMessageHandlerSeeder>()
            .Seed(url, response);
    }

    public static JObject[] EndpointRequests(this IServiceProvider services)
    {
        return services
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
                        streamBytes.Length
                    };

                return JObject.FromObject(new
                {
                    Uri = x.Item1.RequestUri!.PathAndQuery,
                    Method = x.Item1.Method.ToString(),
                    Headers = x.Item1.Headers.Where
                        (kvp => kvp.Key != "x-ms-client-request-id" && kvp.Key != "User-Agent" &&
                              kvp.Key != "Authorization" && kvp.Key != "OpenAI-Organization")
                        .ToDictionary(h => h.Key, h => string.Join(';', h.Value)),
                    ContentType = x.Item1.Content?.Headers.ContentType?.MediaType,
                    Content = contentInformation,
                });
            }).ToArray();
    }


    public static Dictionary<string, object> VerifyRequestsAndResponses(
        this IServiceProvider services,
        object response)
    {
        var validation = new Dictionary<string, object>()
        {
            ["Requests"] = JsonConvert.SerializeObject(services.EndpointRequests(), Formatting.Indented),
            ["Response"] = JsonConvert.SerializeObject(response, Formatting.Indented)
        };
        return validation;
    }

    public static void ClearSeededMessages(this IServiceProvider services)
    {
        services.GetRequiredService<FakeHttpMessageHandlerSeeder>().Clear();
    }


    public static HttpResponseMessage FakeModelErrorResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            error = new[]
            {
                new
                {
                    message =
                        "The server had an error processing your request. Sorry about that! You can retry your request, or contact us through an Azure support request at: https://go.microsoft.com/fwlink/?linkid=2213926 if you keep seeing this error. (Please include the request ID 00000000-0000-0000-0000-000000000000 in your email.)",
                    type = "server_error",
                    param = (string?)null,
                    code = (string?)null
                }
            },
        });

        response.Headers.Add("ms-azureml-model-error-reason", "model_error");
        response.Headers.Add("ms-azureml-model-error-statuscode", "500");

        return response;
    }

    public static HttpResponseMessage FakeChatCompletionsResponse(int? totalTokens = 126)
    {
        var response = new HttpResponseMessage();
        response.Content = new OneTimeStreamReadHttpContent(new
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
        });

        response.Headers.Add("x-ratelimit-remaining-requests", "12");
        response.Headers.Add("x-ratelimit-remaining-tokens", "234");

        return response;
    }

    public static HttpResponseMessage FakeChatCompletionsResponseMultipleChoices()
    {
        var response = new HttpResponseMessage();
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            id = FakeResponseId,
            @object = "chat.completion",
            created = 1679072642,
            model = "gpt-35-turbo",
            usage = new
            {
                prompt_tokens = 29,
                completion_tokens = 30,
                total_tokens = 59
            },
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        role = "assistant",
                        content =
                            "Response one."
                    },
                    finish_reason = "stop",
                    index = 0
                },
                new
                {
                    message = new
                    {
                        role = "assistant",
                        content =
                            "Response two two two."
                    },
                    finish_reason = "stop",
                    index = 1
                }
            },
        });

        response.Headers.Add("x-ratelimit-remaining-requests", "12");
        response.Headers.Add("x-ratelimit-remaining-tokens", "234");

        return response;
    }

    public static HttpResponseMessage FakeCompletionsResponse()
    {
        var response = new HttpResponseMessage();
        response.Content = new OneTimeStreamReadHttpContent(new
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
        });

        return response;
    }

    public static async Task<HttpResponseMessage> FakeStreamingChatCompletionsResponse()
    {
        using var stream =
            new StreamReader(
                typeof(OpenAIFakeResponses).Assembly.GetManifestResourceStream(
                    "OpenAIMockServer.Assets.FakeStreamingResponse.testcontent.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.Content = new ServerSideEventResponse(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        response.Headers.TransferEncodingChunked = true;
        return response;
    }

    public static Task<HttpResponseMessage> FakeEmbeddingArrayResponse()
    {
        var response = new HttpResponseMessage();
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            id = FakeResponseId,
            @object = "list",
            model = "ada",
            usage = new
            {
                prompt_tokens = 58,
                total_tokens = 126
            },
            data = new[]
            {
                new
                {
                    embedding = new [] { 0.1f, 0.2f, 0.3f },
                    index = 0,
                    @object = "embedding"
                },
                new
                {
                    embedding = new [] { 0.4f, 0.5f, 0.6f },
                    index = 1,
                    @object = "embedding"
                }
            }
        });

        return Task.FromResult(response);
    }


    public static Task<HttpResponseMessage> FakeEmbeddingResponse()
    {
        var response = new HttpResponseMessage();
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            id = FakeResponseId,
            @object = "list",
            model = "ada",
            usage = new
            {
                prompt_tokens = 58,
                total_tokens = 126
            },
            data = new[]
            {
                new
                {
                    embedding = new [] { 0.1f, 0.2f, 0.3f },
                    index = 0,
                    @object = "embedding"
                }
            }
        });

        return Task.FromResult(response);
    }

    public static async Task<HttpResponseMessage> FakeStreamingChatCompletionsResponseMultipleChoices()
    {
        using var stream =
            new StreamReader(
                typeof(OpenAIFakeResponses).Assembly.GetManifestResourceStream(
                    "OpenAIMockServer.Assets.FakeOpenAIStreamingResponseMultipleChoices.testcontent.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.Content = new ServerSideEventResponse(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        response.Headers.TransferEncodingChunked = true;
        return response;
    }

    public static async Task<HttpResponseMessage> FakeStreamingCompletionsResponse()
    {
        using var stream =
            new StreamReader(
                typeof(OpenAIFakeResponses).Assembly.GetManifestResourceStream(
                    "OpenAIMockServer.Assets.FakeStreamingCompletionsResponse.testcontent.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.Content = new ServerSideEventResponse(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        response.Headers.TransferEncodingChunked = true;
        return response;
    }

    public static async Task<HttpResponseMessage> FakeStreamingChatCompletionsResponseWithTokenCounts()
    {
        using var stream =
            new StreamReader(
                typeof(OpenAIFakeResponses).Assembly.GetManifestResourceStream(
                    "OpenAIMockServer.Assets.FakeStreamingResponse.with-token-counts.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.Content = new ServerSideEventResponse(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        response.Headers.TransferEncodingChunked = true;
        return response;
    }

    public static async Task<HttpResponseMessage> FakeStreamingCompletionsResponseWithTokenCounts()
    {
        using var stream =
            new StreamReader(
                typeof(OpenAIFakeResponses).Assembly.GetManifestResourceStream(
                    "OpenAIMockServer.Assets.FakeStreamingResponse-completions.with-token-counts.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.Content = new ServerSideEventResponse(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        response.Headers.TransferEncodingChunked = true;
        return response;
    }

    public static async Task<HttpResponseMessage> FakeOpenAIStreamingCompletionsResponse()
    {
        using var stream =
            new StreamReader(
                typeof(OpenAIFakeResponses).Assembly.GetManifestResourceStream(
                    "OpenAIMockServer.Assets.FakeOpenAIStreamingResponse.testcontent.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.Content = new ServerSideEventResponse(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        return response;
    }

    public static HttpResponseMessage FakeAzureOpenAIImageResponse(string openAiUrl)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted);
        response.Headers.Add("operation-location",
            $"https://{openAiUrl}/openai/operations/images/f508bcf2-e651-4b4b-85a7-58ad77981ffa?api-version=2024-02-15-preview");
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            id = "f508bcf2-e651-4b4b-85a7-58ad77981ffa",
            status = "notRunning"
        });

        return response;
    }

    public static HttpResponseMessage FakeAzureOpenAIDALLE3ImageResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            created = 1702525301,
            data = new[]
            {
                new
                {
                    revised_prompt =
                        "A middle-aged computer programmer of ambiguous descent, typing code into a laptop in a spacious, brightly lit living room. Regardless of gender, they bear a somewhat weary look reflecting their extensive experience in their profession. Their room is illuminated by the warm sunbeams filtering through the window.",
                    url = "https://somewhere-else.com"
                }
            },
            id = "f508bcf2-e651-4b4b-85a7-58ad77981ffa",
            status = "notRunning",
        });

        return response;
    }


    public static HttpResponseMessage FakeOpenAIDALLE3ImageResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            created = 1702525301,
            data = new[]
            {
                new
                {
                    url = "https://somewhere-else.com"
                }
            }
        });

        return response;
    }

    public static HttpResponseMessage FakeAzureOpenAIAssistantResponse(string assistantName)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var created = 1702525391;
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            id = assistantName,
            @object = "assistant",
            created_at = created,
            name = "fred fibnar",
            model = "gpt-4",
            instructions = "You are Fred"
        });

        return response;
    }

    public static HttpResponseMessage FakeMessageResponse(string threadId)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var created = 1702525391;
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            id = "msg_123",
            @object = "thread.message",
            created_at = created,
            thread_id = threadId,
            role = "user",
            content = new[]
            {
                new
                {
                    type = "text",
                    text = new
                    {
                        value = "test message",
                        annotations = Array.Empty<object>()
                    }
                }
            }
        });

        return response;
    }

    public static HttpResponseMessage FakeAzureOpenAIImageStatusResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var created = 1702525391;
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            id = "f508bcf2-e651-4b4b-85a7-58ad77981ffa",
            created,
            status = "succeeded",
            result = new
            {
                created,
                data = new[]
                {
                    new
                    {
                        url = "https://images.localtest.me/some-image-somehere"
                    }
                }
            }
        });

        return response;
    }

    public static HttpResponseMessage FakeOpenAIAudioTranscriptionResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new OneTimeStreamReadHttpContent("""
                                                            1
                                                            00:00:00,000 --> 00:00:07,000
                                                            I wonder what the translation will be for this
                                                            """.ReplaceLineEndings("\n"));

        response.Headers.Add("openai-processing-ms", "744");
        response.Headers.Add("openai-version", "2020-10-01");
        response.Headers.Add("x-ratelimit-limit-requests", "50");
        response.Headers.Add("x-ratelimit-remaining-requests", "49");
        response.Headers.Add("x-ratelimit-reset-requests", "1.2s");
        return response;
    }

    public static HttpResponseMessage FakeOpenAIAudioTranslationResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new OneTimeStreamReadHttpContent("""
                                                            {
                                                              "text": "I wonder what the translation will be for this"
                                                            }
                                                            """.ReplaceLineEndings("\n"));

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
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            error = new
            {
                code = "DeploymentNotFound",
                message =
                    "The API deployment for this resource does not exist. If you created the deployment within the last 5 minutes, please wait a moment and try again."
            }
        });
        return response;
    }

    public static HttpResponseMessage InternalServerErrorResponse()
    {
        var response = new HttpResponseMessage();
        response.StatusCode = HttpStatusCode.InternalServerError;
        return response;
    }

    public static HttpResponseMessage RateLimitResponse(TimeSpan retryAfter)
    {
        var response = new HttpResponseMessage();
        response.StatusCode = HttpStatusCode.TooManyRequests;
        response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter);
        return response;
    }

    private class ServerSideEventResponse(string knownContent) : HttpContent
    {
        private readonly string[] _knownContentLines = knownContent.ReplaceLineEndings("\n").Split("\n");

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await using var writer = new StreamWriter(stream, leaveOpen: true);
            foreach (var line in _knownContentLines)
            {
                await writer.WriteAsync($"{line}\n");
                await writer.FlushAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(5));
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private class OneTimeStreamReadHttpContent : HttpContent
    {
        private readonly Stream _backingStream;
        private bool _read;

        public OneTimeStreamReadHttpContent(object jsonResponse)
        {
            _backingStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonResponse)));
            Headers.ContentType = new MediaTypeHeaderValue("application/json", "utf-8");
        }

        public OneTimeStreamReadHttpContent(string textResponse)
        {
            _backingStream = new MemoryStream(Encoding.UTF8.GetBytes(textResponse));
            Headers.ContentType = new MediaTypeHeaderValue("text/plain", "utf-8");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            if (_read)
            {
                throw new InvalidOperationException("Already read");
            }

            _read = true;
            return _backingStream.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
        {
            if (_read)
            {
                throw new InvalidOperationException("Already read");
            }

            _read = true;
            return _backingStream;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            if (_read)
            {
                throw new InvalidOperationException("Already read");
            }

            _read = true;
            return Task.FromResult(_backingStream);
        }

        protected override void Dispose(bool disposing)
        {
            _backingStream.Dispose();
            base.Dispose(disposing);
        }
    }
}