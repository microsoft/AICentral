using AICentralOpenAIMock;
using Microsoft.Extensions.DependencyInjection;

namespace OpenAIMock;

public static class OpenAITestEx
{
    public const string OpenAIClientApiVersion = "2024-04-01-preview";

    /// <summary>
    /// Adds a fake Open AI handler for you to seed responses to.
    /// </summary>
    /// <remarks>
    /// You can grab the FakeHttpMessageHandlerSeeder from your service provider to see the Requests sent to it. This can be useful for verification tests. 
    /// </remarks>
    /// <param name="serviceCollection"></param>
    /// <param name="clientName"></param>
    /// <returns></returns>
    public static string RegisterOpenAIMockHandler(this IServiceCollection serviceCollection, string clientName)
    {
        var key = Guid.NewGuid().ToString();
        var seeder = new FakeHttpMessageHandlerSeeder();
        serviceCollection.AddKeyedSingleton(key, seeder);
        serviceCollection.AddHttpClient(clientName).ConfigurePrimaryHttpMessageHandler<FakeHttpMessageHandler>();
        return key;
    }
}