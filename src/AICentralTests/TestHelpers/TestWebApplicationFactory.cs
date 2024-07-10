using AICentral;
using AICentral.Core;
using AICentralOpenAIMock;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAIMock;
using Xunit.Abstractions;

namespace AICentralTests.TestHelpers;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, ITestOutputHelperAccessor
    where TProgram : class
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("tests");
        
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILoggerFactory>(new LoggerFactory(new[]
                {
                    new XUnitLoggerProvider(this, new XUnitLoggerOptions())
                },
                new LoggerFilterOptions()
                {
                    MinLevel = LogLevel.Trace
                }));

            services.PostConfigure<AICentralConfig>(cfg => cfg.EnableDiagnosticsHeaders = true);

            services.Remove(services.Single(x => x.ServiceType == typeof(ConfiguredPipelines)));

            var pipelines = new[]
            {
                TestPipelines.AzureOpenAIServiceWithAuth(),
                TestPipelines.AzureOpenAIServiceWithPriorityEndpointPickerNoAuth(),
                TestPipelines.AzureOpenAIServiceWithSingleOpenAIEndpoint(),
                TestPipelines.AzureOpenAIServiceWithRandomAzureOpenAIEndpoints(),
                TestPipelines.AzureOpenAIServiceWithSingleAzureOpenAIEndpoint(),
                TestPipelines.AzureOpenAIServiceWithRateLimitingAndSingleEndpoint(),
                TestPipelines.AzureOpenAIServiceWithBulkHeadOnPipelineAndSingleEndpoint(),
                TestPipelines.AzureOpenAIServiceWithBulkHeadOnSingleEndpoint(),
                TestPipelines.AzureOpenAILowestLatencyEndpoint(),
                TestPipelines.AzureOpenAIServiceWithSingleEndpointSelectorHierarchy(),
                TestPipelines.AzureOpenAIServiceWithClientPartitionedRateLimiter(),
                TestPipelines.AzureOpenAIServiceWithTokenRateLimitingAndSingleEndpoint(),
                TestPipelines.AzureOpenAIServiceWithClientPartitionedTokenRateLimiter(),
                TestPipelines.AzureOpenAIServiceWithRandomOpenAIEndpoints(),
                TestPipelines.AzureOpenAIServiceWithRandomOpenAIEndpointsDifferentModelMappings(),
                TestPipelines.AzureOpenAIServiceWith404Endpoint(),
                TestPipelines.AzureOpenAIServiceWithRandomAffinityBasedAzureOpenAIEndpoints(),
                TestPipelines.AzureOpenAIServiceWithSingleAzureOpenAIEndpointWithMappedModel(),
                TestPipelines.AzureOpenAIServiceWithInBuiltJwtAuth(),
                TestPipelines.AzureOpenAIServiceWithInBuiltWildcardJwtAuth(),
                TestPipelines.AzureOpenAIServiceWithAutoUserPopulation(),
                TestPipelines.TokenPlusKeyEndpoint(),
                TestPipelines.AzureOpenAIServiceWithChatImageFiltering()
            };

            var assembler = pipelines.Aggregate(pipelines[0], (prev, current) => prev.CombineAssemblers(current));
            var seeder = new FakeHttpMessageHandlerSeeder();
            assembler.AddServices(services, new FakeHttpMessageHandler(seeder), NullLogger.Instance);
            services.AddSingleton(seeder);

            var fakeDateTimeProvider = new FakeDateTimeProvider();
            services.AddSingleton<IDateTimeProvider>(fakeDateTimeProvider);
            services.AddSingleton(fakeDateTimeProvider);
            
        });

        return base.CreateHost(builder);
    }

    public ITestOutputHelper? OutputHelper { get; set; }

}