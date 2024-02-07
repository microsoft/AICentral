using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AICentralTests.Endpoints;
using Newtonsoft.Json;

namespace AICentralTests.TestHelpers;

public class AICentralFakeResponses
{
    public static readonly string Endpoint500 = Guid.NewGuid().ToString();
    public static readonly string Endpoint404 = Guid.NewGuid().ToString();
    public static readonly string Endpoint200 = Guid.Parse("47bae1ca-d2f0-4584-b2ac-9897e7088919").ToString();
    public static readonly string Endpoint200Number2 = Guid.Parse("84bae1ca-d2f0-4584-b2ac-9897e708891a").ToString();
    public static readonly string FastEndpoint = Guid.NewGuid().ToString();
    public static readonly string SlowEndpoint = Guid.NewGuid().ToString();
    public static readonly string FakeResponseId = "chatcmpl-6v7mkQj980V1yBec6ETrKPRqFjNw9";

    public static HttpResponseMessage FakeModelErrorResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        response.Content = new OneTimeStreamReadHttpContent(new
        {
            error = new[]
            {
                new
                {
                    message = "The server had an error processing your request. Sorry about that! You can retry your request, or contact us through an Azure support request at: https://go.microsoft.com/fwlink/?linkid=2213926 if you keep seeing this error. (Please include the request ID 00000000-0000-0000-0000-000000000000 in your email.)",
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


    public static async Task<HttpResponseMessage> FakeStreamingCompletionsResponse()
    {
        using var stream =
            new StreamReader(
                typeof(the_azure_openai_pipeline).Assembly.GetManifestResourceStream(
                    "AICentralTests.Assets.FakeStreamingResponse.testcontent.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.Content = new SSEResponse(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        response.Headers.TransferEncodingChunked = true;
        return response;
    }

    public static async Task<HttpResponseMessage> FakeOpenAIStreamingCompletionsResponse()
    {
        using var stream =
            new StreamReader(
                typeof(the_azure_openai_pipeline).Assembly.GetManifestResourceStream(
                    "AICentralTests.Assets.FakeOpenAIStreamingResponse.testcontent.txt")!);

        var content = await stream.ReadToEndAsync();
        var response = new HttpResponseMessage();
        response.Content = new SSEResponse(content);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
        return response;
    }

    public static HttpResponseMessage FakeAzureOpenAIImageResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted);
        response.Headers.Add("operation-location",
            $"https://{Endpoint200}/openai/operations/images/f508bcf2-e651-4b4b-85a7-58ad77981ffa?api-version=2023-12-01-preview");
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

    public static HttpResponseMessage FakeAzureOpenAIImageStatusResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var created = 1702525391;
        response.Content = new OneTimeStreamReadHttpContent(new
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

    private class SSEResponse : HttpContent
    {
        private readonly string[] _knownContentLines;

        public SSEResponse(string knownContent)
        {
            _knownContentLines = knownContent.ReplaceLineEndings("\n").Split("\n");
            //_length = Encoding.UTF8.GetBytes(knownContent).LongLength;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await using var writer = new StreamWriter(stream, leaveOpen: true);
            foreach (var line in _knownContentLines)
            {
                await writer.WriteAsync($"{line}\n");
                await writer.FlushAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(25));
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