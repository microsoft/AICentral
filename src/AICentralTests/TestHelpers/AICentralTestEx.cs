using System.Text;
using Newtonsoft.Json;

namespace AICentralTests.TestHelpers;

public static class AICentralTestEx
{
    public static Task<HttpResponseMessage> PostChatCompletions(this HttpClient client, string endpointName, string modelName = "Model1")
    {
        return client.PostAsync(
            $"https://{endpointName}.localtest.me/openai/deployments/Model1/chat/completions?api-version=2023-12-01-preview",
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