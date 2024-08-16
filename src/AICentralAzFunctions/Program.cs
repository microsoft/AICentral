using AICentral.AzureAISearchVectorizationProxy;
using AICentral.Configuration;
using AICentral.Logging.AzureMonitor.AzureMonitorLogging;
using AICentral.Logging.PIIStripping;
using AICentral.RateLimiting.DistributedRedis;
using AICentralAzFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSimpleConsole(options =>
{
    options.ColorBehavior = LoggerColorBehavior.Default;
    options.SingleLine = true;
}));
var startupLogger = loggerFactory.CreateLogger("AICentral.Startup");

var builder = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureHostConfiguration(c =>
    {
        if (Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") != null)
        {
            c.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT")}.json");
        }
    })
    .ConfigureServices((hc, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        services.AddAICentral(
           hc.Configuration,
            startupLogger: startupLogger,
            additionalComponentAssemblies:
            [
                typeof(AzureMonitorLoggerFactory).Assembly,
                typeof(PIIStrippingLogger).Assembly,
                typeof(DistributedRateLimiter).Assembly,
                typeof(AdaptJsonToAzureAISearchTransformer).Assembly
            ]);
    });

var host = builder.Build();

host.Run();
