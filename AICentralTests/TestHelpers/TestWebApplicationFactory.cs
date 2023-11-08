using System.Drawing.Printing;
using AICentral;
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

            var pipelines = new[]
            {
                TestPipelines.ApiKeyAuth(),
                TestPipelines.RandomEndpointPickerNoAuth(),
                TestPipelines.PriorityEndpointPickerNoAuth(),
                TestPipelines.OpenAIService(),
                TestPipelines.WithOpenAIEndpoint(),
                TestPipelines.AzureOpenAICatchAllEndpoint()
            };

            var assembler = pipelines.Aggregate(pipelines[0], (prev, current) => prev.CombineAssemblers(current));
            assembler.AddServices(services, NullLogger.Instance);

            var fakeClient = new HttpClient(new FakeHttpMessageHandler());
            services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(fakeClient));
        });
        return base.CreateHost(builder);
    }

    public ITestOutputHelper? OutputHelper { get; set; }

    class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _fakeClient;

        public FakeHttpClientFactory(HttpClient fakeClient)
        {
            _fakeClient = fakeClient;
        }

        public HttpClient CreateClient(string name)
        {
            return _fakeClient;
        }
    }
}