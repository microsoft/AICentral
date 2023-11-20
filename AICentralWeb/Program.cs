using AICentral;
using AICentral.Configuration;
using AICentral.Logging.AzureMonitor;
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
    additionalComponentAssemblies: typeof(AzureMonitorLoggerBuilder).Assembly);

builder.Services.AddRazorPages();

var app = builder.Build();

app.MapRazorPages();

app.UseAICentral();

app.Run();

public partial class Program
{
}