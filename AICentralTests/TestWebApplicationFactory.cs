using AICentral.Configuration;
using AICentral.Pipelines;
using AICentral.Pipelines.Auth;
using AICentral.Pipelines.Endpoints;
using AICentral.Pipelines.EndpointSelectors.Random;
using AICentral.Pipelines.Routes;
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
            services.Remove(services.Single(x => x.ServiceType == typeof(AICentral.Configuration.AICentralPipelines)));
            services.AddSingleton(
                new AICentralPipelines(
                    new[]
                    {
                        new AICentralPipeline(
                            "Test",
                            new SimplePathMatchRouter("/openai/deployments/random/chat/completions"),
                            new NoClientAuthAuthRuntime(),
                            Array.Empty<IAICentralPipelineStepRuntime>(),
                            new RandomEndpointSelectorRuntime(new[]
                            {
                                AICentralTestEndpointBuilder.Random()
                            }))
                    }));

            services.Remove(services.Single(x => x.ServiceType == typeof(IAIEndpointDispatcher)));
            services.AddTransient<IAIEndpointDispatcher, FakeEndpointDispatcher>();
        });
        return base.CreateHost(builder);
    }
}