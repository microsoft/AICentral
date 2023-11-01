using AICentral;
using AICentral.Configuration;
using AICentral.PipelineComponents.Auth.AllowAnonymous;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.AzureOpenAI;
using AICentral.PipelineComponents.EndpointSelectors.Random;
using AICentral.PipelineComponents.Routes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AICentralTests;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Remove(services.Single(x => x.ServiceType == typeof(AICentralPipelines)));
            services.AddSingleton(
                new AICentralPipelines(
                    new[]
                    {
                        new AICentralPipeline(
                            "Test",
                            new SimplePathMatchRouter("/openai/deployments/random/chat/completions"),
                            new AllowAnonymousClientAuthProvider(),
                            Array.Empty<IAICentralPipelineStep>(),
                            new RandomEndpointSelector(new IAICentralEndpointDispatcher[]
                            {
                                AICentralTestEndpointBuilder.Random()
                            }))
                    }));

            services.AddHttpClient<HttpAIEndpointDispatcher>();
            services.AddTransient<IAIEndpointDispatcher, FakeEndpointDispatcher>();
        });
        return base.CreateHost(builder);
    }
}