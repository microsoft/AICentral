using AICentral;
using AICentral.Configuration;
using AICentral.Logging.PIIStripping;
using AICentral.RateLimiting.DistributedRedis;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Trace;

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

using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole());
var startupLogger = loggerFactory.CreateLogger("AICentral.Startup");

builder.Services.AddCors();

builder.Services.AddAICentral(
    builder.Configuration,
    startupLogger: startupLogger,
    additionalComponentAssemblies:
    [
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