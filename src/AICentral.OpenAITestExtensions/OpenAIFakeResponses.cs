using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

    public static HttpResponseMessage FakeChatCompletionsResponse(int? totalTokens = 126, int remainingRequests = 12, int remainingTokens = 234)
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

        response.Headers.Add("x-ratelimit-remaining-requests", remainingRequests.ToString());
        response.Headers.Add("x-ratelimit-remaining-tokens", remainingTokens.ToString());

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
                    "AICentral.OpenAITestExtensions.Assets.FakeStreamingResponse.testcontent.txt")!);

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

    public static Task<HttpResponseMessage> FakeBase64EmbeddingResponse()
    {
        var response = new HttpResponseMessage();
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            @object = "list",
            data = new[]
            {
                new
                {
                    embedding = "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",
                    index = 0,
                    @object = "embedding"
                }
            },
            model = "ada",
            usage = new
            {
                prompt_tokens = 4,
                total_tokens = 4
            }
        });

        return Task.FromResult(response);    }

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
                    "AICentral.OpenAITestExtensions.Assets.FakeOpenAIStreamingResponseMultipleChoices.testcontent.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.Content = new ServerSideEventResponse(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        response.Headers.TransferEncodingChunked = true;
        return response;
    }

    public static async Task<HttpResponseMessage> FakeStreamingChatCompletionsResponseContentFilter()
    {
        using var stream =
            new StreamReader(
                typeof(OpenAIFakeResponses).Assembly.GetManifestResourceStream(
                    "AICentral.OpenAITestExtensions.Assets.FakeContentFilterTrigger.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.Content = new ServerSideEventResponse(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        response.Headers.TransferEncodingChunked = true;
        return response;
    }

    public static async Task<HttpResponseMessage> FakeContentFilterJailbreakAttempt()
    {
        using var stream =
            new StreamReader(
                typeof(OpenAIFakeResponses).Assembly.GetManifestResourceStream(
                    "AICentral.OpenAITestExtensions.Assets.FakeContentFilterJailbreak.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.StatusCode = HttpStatusCode.BadRequest;
        response.Content = new StringContent(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return response;
    }

    public static async Task<HttpResponseMessage> FakeStreamingCompletionsResponse()
    {
        using var stream =
            new StreamReader(
                typeof(OpenAIFakeResponses).Assembly.GetManifestResourceStream(
                    "AICentral.OpenAITestExtensions.Assets.FakeStreamingCompletionsResponse.testcontent.txt")!);

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
                    "AICentral.OpenAITestExtensions.Assets.FakeStreamingResponse.with-token-counts.txt")!);

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
                    "AICentral.OpenAITestExtensions.Assets.FakeStreamingResponse-completions.with-token-counts.txt")!);

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
                    "AICentral.OpenAITestExtensions.Assets.FakeOpenAIStreamingResponse.testcontent.txt")!);

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