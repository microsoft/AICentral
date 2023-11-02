using AICentral;
using AICentral.PipelineComponents.Endpoints.OpenAI;
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
            var randomSelector = TestPipelines.RandomEndpointPickerNoAuth();
            var prioritised = TestPipelines.PriorityEndpointPickerNoAuth();

            var assembler = simplePathMatch
                .CombineAssemblers(randomSelector)
                .CombineAssemblers(prioritised);
            assembler.AddServices(services, NullLogger.Instance);

            services.AddHttpClient<HttpAIEndpointDispatcher>();
            services.AddHttpClient<HttpAIEndpointDispatcher>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new FakeHttpMessageHandler(AICentralTestEndpointBuilder.FakeResponse()));
        });
        return base.CreateHost(builder);
    }
}