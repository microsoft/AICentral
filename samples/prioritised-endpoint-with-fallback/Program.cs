using AICentral.Configuration;
using AICentral.Logging.AzureMonitor.AzureMonitorLogging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo
    .Console()
    .CreateLogger();

builder.Host.UseSerilog(logger);

builder.Services.AddAICentral(
    builder.Configuration,
    startupLogger: new SerilogLoggerProvider(logger).CreateLogger("AICentralStartup"),
    additionalComponentAssemblies:
    [
        typeof(AzureMonitorLoggerFactory).Assembly,
    ]);

var app = builder.Build();

app.UseAICentral();

app.Run();

namespace AICentralWeb
{
    public partial class Program
    {
    }
}