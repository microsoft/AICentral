using AICentral.Configuration;
using AICentral.Pipelines;
using AICentral.Pipelines.Endpoints;
using AICentral.Pipelines.EndpointSelectors.Random;
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
            services.Remove(services.Single(x => x.ServiceType == typeof(AICentral.Configuration.AICentral)));
            services.AddSingleton(new AICentral.Configuration.AICentral(new AICentralOptions()
            {
                Pipelines = new List<AICentralPipeline>()
                {
                    AICentralTestEndpointBuilder.Build(
                        new RandomEndpointSelector(new List<IAICentralEndpoint>()
                            { AICentralTestEndpointBuilder.Random() }), "/openai/deployments/random/chat/completions")
                }
            }));

            services.Remove(services.Single(x => x.ServiceType == typeof(IAIEndpointDispatcher)));
            services.AddTransient<IAIEndpointDispatcher, FakeEndpointDispatcher>();
        });

        return base.CreateHost(builder);
    }
}