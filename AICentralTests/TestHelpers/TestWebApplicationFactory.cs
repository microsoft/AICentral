using AICentral;
using AICentral.PipelineComponents.Auth.AllowAnonymous;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.EndpointSelectors.Priority;
using AICentral.PipelineComponents.EndpointSelectors.Random;
using AICentral.PipelineComponents.Routes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AICentralTests.TestHelpers;

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
                            new SimplePathMatchRouter("/openai/deployments/random/{*prefix}"),
                            new AllowAnonymousClientAuthProvider(),
                            Array.Empty<IAICentralPipelineStep>(),
                            new RandomEndpointSelector(new IAICentralEndpointDispatcher[]
                            {
                                AICentralTestEndpointBuilder.Success200()
                            })),
                        new AICentralPipeline(
                            "Test",
                            new SimplePathMatchRouter("/openai/deployments/priority/{*prefix}"),
                            new AllowAnonymousClientAuthProvider(),
                            Array.Empty<IAICentralPipelineStep>(),
                            new PriorityEndpointSelector(
                                new RandomEndpointSelector(
                                    new IAICentralEndpointDispatcher[]
                                    {
                                        AICentralTestEndpointBuilder.FailingModelNotFound()
                                    }),
                                new RandomEndpointSelector(new[]
                                {
                                    AICentralTestEndpointBuilder.Success200()
                                }))),
                    }));

            services.AddHttpClient<HttpAIEndpointDispatcher>();
            services.AddHttpClient<HttpAIEndpointDispatcher>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new FakeHttpMessageHandler(AICentralTestEndpointBuilder.FakeResponse()));
        });
        return base.CreateHost(builder);
    }
}