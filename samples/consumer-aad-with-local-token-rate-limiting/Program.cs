using AICentral;
using AICentral.Configuration;
using AICentral.Logging.AzureMonitor.AzureMonitorLogging;
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(ActivitySource.AICentralTelemetryName);
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource(ActivitySource.AICentralTelemetryName);
    })
    .UseAzureMonitor();

builder.Services.AddAICentral(
    builder.Configuration,
    additionalComponentAssemblies:
    [
        typeof(AzureMonitorLoggerFactory).Assembly,
    ]);

var app = builder.Build();

app.UseAICentral();

app.Run();