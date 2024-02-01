using AICentral;
using AICentral.Configuration;
using AICentral.Core;
using AICentral.Logging.AzureMonitor.AzureMonitorLogging;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.EnvironmentName != "tests")
{
    builder.Services
        .AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddMeter(ActivitySource.AICentralTelemetryName);
        })
        .WithTracing(tracing =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // We want to view all traces in development
                tracing.SetSampler(new AlwaysOnSampler());
            }

            tracing.AddAspNetCoreInstrumentation()
                .AddSource(ActivitySource.AICentralTelemetryName);
        })
        .UseAzureMonitor();
}

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

builder.Services.AddRazorPages();

var app = builder.Build();

app.MapRazorPages();

app.UseAICentral();

app.Run();

namespace AICentralWeb
{
    public partial class Program
    {
    }
}