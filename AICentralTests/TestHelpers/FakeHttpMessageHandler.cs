using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AICentralTests.TestHelpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private long _bulkHeadCount;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri!.AbsoluteUri.Equals("https://api.openai.com/v1/chat/completions"))
        {
            return AICentralFakeResponses.FakeChatCompletionsResponse();
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.EndpointBulkHeadOnPipeline}/openai/deployments/Model1/chat/completions?api-version=2023-05-15") ||
            request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.EndpointBulkHeadOnEndpoint}/openai/deployments/Model1/chat/completions?api-version=2023-05-15"))
        {
            if (Interlocked.Read(ref _bulkHeadCount) == 5)
            {
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            }

            Interlocked.Increment(ref _bulkHeadCount);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            Interlocked.Decrement(ref _bulkHeadCount);
            return AICentralFakeResponses.FakeCompletionsResponse();
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint404}/openai/deployments/Model1/chat/completions?api-version=2023-05-15"))
        {
            return AICentralFakeResponses.NotFoundResponse();
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint500}/openai/deployments/Model1/chat/completions?api-version=2023-05-15"))
        {
            return AICentralFakeResponses.InternalServerErrorResponse();
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200}/openai/deployments/Model1/chat/completions?api-version=2023-05-15"))
        {
            var requestContent =
                (JObject)JsonConvert.DeserializeObject(await request.Content!.ReadAsStringAsync(cancellationToken))!;
            if (requestContent["model"] != null)
                throw new InvalidOperationException("OpenAI parameter passed to Azure endpoint");
            if (requestContent["messages"] == null)
                throw new InvalidOperationException(
                    "Request with no messages came through to chat completions endpoint");
            return AICentralFakeResponses.FakeChatCompletionsResponse();
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200}/openai/deployments/Model1/completions?api-version=2023-05-15"))
        {
            return AICentralFakeResponses.FakeCompletionsResponse();
        }


        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200Number2}/openai/deployments/Model1/chat/completions?api-version=2023-05-15"))
        {
            return AICentralFakeResponses.FakeChatCompletionsResponse();
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200}/openai/images/generations:submit?api-version=2023-09-01-preview"))
        {
            return AICentralFakeResponses.FakeAzureOpenAIImageResponse();
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200}/openai/operations/images/f508bcf2-e651-4b4b-85a7-58ad77981ffa?api-version=2023-09-01-preview"))
        {
            return AICentralFakeResponses.FakeAzureOpenAIImageStatusResponse();
        }

        if (request.RequestUri.AbsoluteUri.Equals(
                $"https://api.openai.com/v1/audio/transcriptions"))
        {
            return AICentralFakeResponses.FakeOpenAIAudioTranscriptionResponse();
        }

        if (request.RequestUri.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200}/openai/deployments/whisper-1/audio/transcriptions?api-version=2023-05-15"))
        {
            return AICentralFakeResponses.FakeOpenAIAudioTranscriptionResponse();
        }

        throw new NotSupportedException($"No fake response registered for {request.RequestUri.AbsoluteUri}");
    }
}