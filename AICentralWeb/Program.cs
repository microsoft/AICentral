using AICentral;
using AICentral.Configuration;
using AICentral.Logging.AzureMonitor;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddMeter(AICentralActivitySource.AICentralTelemetryName);
        })
        .WithTracing(tracing =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // We want to view all traces in development
                tracing.SetSampler(new AlwaysOnSampler());
            }

            tracing.AddAspNetCoreInstrumentation()
                .AddSource(AICentralActivitySource.AICentralTelemetryName);

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
    additionalComponentAssemblies: typeof(AzureMonitorLoggerFactory).Assembly);

builder.Services.AddRazorPages();

var app = builder.Build();

app.MapRazorPages();

app.UseAICentral();

app.Run();

public partial class Program
{
}