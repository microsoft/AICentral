using AICentral;
using AICentral.PipelineComponents.Endpoints;
using AICentral.PipelineComponents.Endpoints.AzureOpenAI;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace AICentralTests.TestHelpers;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, ITestOutputHelperAccessor where TProgram : class
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILoggerFactory>(new LoggerFactory(new[]
            {
                new XUnitLoggerProvider(this, new XUnitLoggerOptions())
            }, new LoggerFilterOptions()
            {
                MinLevel = LogLevel.Trace
            }));

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

    public ITestOutputHelper? OutputHelper { get; set; }
}