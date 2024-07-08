using AICentral;
using AICentral.Configuration;
using AICentral.DistributedTokenLimits;
using AICentral.Logging.AzureMonitor.AzureMonitorLogging;
using AICentral.RateLimiting.DistributedRedis;
using Azure.Monitor.OpenTelemetry.AspNetCore;
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
            metrics.AddMeter(ActivitySource.AICentralTelemetryName);
        })
        .WithTracing(tracing =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // We want to view all traces in development
                tracing.SetSampler(new AlwaysOnSampler());
            }

            tracing.AddSource(ActivitySource.AICentralTelemetryName);
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

builder.Services.AddCors();

builder.Services.AddAICentral(
    builder.Configuration,
    startupLogger: new SerilogLoggerProvider(logger).CreateLogger("AICentralStartup"),
    additionalComponentAssemblies:
    [
        typeof(AzureMonitorLoggerFactory).Assembly,
        typeof(PIIStrippingLogger).Assembly,
        typeof(DistributedRateLimiter).Assembly,
    ]);

var enableSummaryPage = builder.Configuration.GetValue<bool>("EnableAICentralSummaryWebPage");

if (enableSummaryPage)
{
    builder.Services.AddRazorPages();
}

var app = builder.Build();

if (enableSummaryPage)
{
    app.MapRazorPages();
}

app.UseCors(corsPolicyBuilder => corsPolicyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAICentral();

app.Run();

namespace AICentralWeb
{
    public partial class Program
    {
    }
}