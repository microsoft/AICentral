namespace AICentralTests.TestHelpers;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri!.AbsoluteUri.Equals("https://api.openai.com/v1/chat/completions"))
        {
            return Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse());
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint404}/openai/deployments/Model1/chat/completions?api-version=2023-05-15"))
        {
            return Task.FromResult(AICentralFakeResponses.NotFoundResponse());
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint500}/openai/deployments/Model1/chat/completions?api-version=2023-05-15"))
        {
            return Task.FromResult(AICentralFakeResponses.InternalServerErrorResponse());
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200}/openai/deployments/Model1/chat/completions?api-version=2023-05-15"))
        {
            return Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse());
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200}/openai/deployments/Model1/completions?api-version=2023-05-15"))
        {
            return Task.FromResult(AICentralFakeResponses.FakeCompletionsResponse());
        }


        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200Number2}/openai/deployments/Model1/chat/completions?api-version=2023-05-15"))
        {
            return Task.FromResult(AICentralFakeResponses.FakeChatCompletionsResponse());
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200}/openai/images/generations:submit?api-version=2023-09-01-preview"))
        {
            return Task.FromResult(AICentralFakeResponses.FakeAzureOpenAIImageResponse());
        }

        if (request.RequestUri!.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200}/openai/operations/images/f508bcf2-e651-4b4b-85a7-58ad77981ffa?api-version=2023-09-01-preview"))
        {
            return Task.FromResult(AICentralFakeResponses.FakeAzureOpenAIImageStatusResponse());
        }

        if (request.RequestUri.AbsoluteUri.Equals(
                $"https://api.openai.com/v1/audio/transcriptions"))
        {
            return Task.FromResult(AICentralFakeResponses.FakeOpenAIAudioTranscriptionResponse());
        }

        if (request.RequestUri.AbsoluteUri.Equals(
                $"https://{AICentralFakeResponses.Endpoint200}/openai/deployments/whisper-1/audio/transcriptions?api-version=2023-05-15"))
        {
            return Task.FromResult(AICentralFakeResponses.FakeOpenAIAudioTranscriptionResponse());
        }

        throw new NotSupportedException($"No fake response registered for {request.RequestUri.AbsoluteUri}");
    }
}