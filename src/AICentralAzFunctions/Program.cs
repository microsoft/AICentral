using AICentralAzFunctions.Quickstarts;
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
    .ConfigureServices((hc, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddAuthentication();
        services.AddAuthorization();
        
        var config = new APImProxyWithCosmosLogging.Config();
        hc.Configuration.Bind("AICentral", config);
        var assembler = APImProxyWithCosmosLogging.BuildAssembler(config);
        assembler.AddServices(services, null, startupLogger);
    });

var host = builder.Build();

host.Run();

