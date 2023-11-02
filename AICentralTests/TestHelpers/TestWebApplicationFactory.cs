using AICentral;
using AICentral.PipelineComponents.Auth.AllowAnonymous;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.OpenAI;
using AICentral.PipelineComponents.EndpointSelectors.Priority;
using AICentral.PipelineComponents.EndpointSelectors.Random;
using AICentral.PipelineComponents.Routes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AICentralTests.TestHelpers;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Remove(services.Single(x => x.ServiceType == typeof(AICentralPipelines)));

            var simplePathMatch = TestPipelines.ApiKeyAuth();
            simplePathMatch.AddServices(services, NullLogger.Instance);
            var assembledPipelinesServiceDescriptor = services.Single(x => x.ServiceType == typeof(AICentralPipelines));
            services.Remove(assembledPipelinesServiceDescriptor);
            var assembledPipelines = (AICentralPipelines)assembledPipelinesServiceDescriptor.ImplementationInstance!;

            //will add and build pipeplines... let's just splice a few more in!
            var aiCentralPipelines = new AICentralPipelines(
                new[]
                {
                    new AICentralPipeline(
                        "Random",
                        new SimplePathMatchRouter("/openai/deployments/random/{*prefix}"),
                        new AllowAnonymousClientAuthProvider(),
                        Array.Empty<IAICentralPipelineStep>(),
                        new RandomEndpointSelector(new IAICentralEndpointDispatcher[]
                        {
                            AICentralTestEndpointBuilder.Success200()
                        })),
                    new AICentralPipeline(
                        "Priority",
                        new SimplePathMatchRouter("/openai/deployments/priority/{*prefix}"),
                        new AllowAnonymousClientAuthProvider(),
                        Array.Empty<IAICentralPipelineStep>(),
                        new PriorityEndpointSelector(
                            new IAICentralEndpointDispatcher[]
                            {
                                AICentralTestEndpointBuilder.FailingModelNotFound(),
                                AICentralTestEndpointBuilder.FailingModelInternalServerError(),
                            },
                            new[]
                            {
                                AICentralTestEndpointBuilder.Success200()
                            }))
                });

            var allPipelines = assembledPipelines.Pipelines.Union(aiCentralPipelines.Pipelines).ToArray();
            services.AddSingleton(new AICentralPipelines(allPipelines));

            services.AddHttpClient<HttpAIEndpointDispatcher>();
            services.AddHttpClient<HttpAIEndpointDispatcher>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new FakeHttpMessageHandler(AICentralTestEndpointBuilder.FakeResponse()));
        });
        return base.CreateHost(builder);
    }
}