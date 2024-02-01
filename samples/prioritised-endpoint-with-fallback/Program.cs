using AICentral.Configuration;
using AICentral.Logging.AzureMonitor.AzureMonitorLogging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAICentral(
    builder.Configuration,
    additionalComponentAssemblies:
    [
        typeof(AzureMonitorLoggerFactory).Assembly,
    ]);

var app = builder.Build();

app.UseAICentral();

app.Run();