using System.Text;
using Newtonsoft.Json;
using OpenAIMock;

namespace AICentralTests.TestHelpers;

internal static class HttpEx
{
    public static Task<HttpResponseMessage> PostChatCompletions(this HttpClient client, string endpointName, string modelName = "Model1")
    {
        return client.PostAsync(
            $"https://{endpointName}.localtest.me/openai/deployments/{modelName}/chat/completions?api-version={OpenAITestEx.OpenAIClientApiVersion}",
            new StringContent(JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "Does Azure OpenAI support customer managed keys?" },
                },
                max_tokens = 5
            }), Encoding.UTF8, "application/json"));
    }
}