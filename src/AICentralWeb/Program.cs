using AICentral;
using AICentral.Configuration;
using AICentral.DistributedTokenLimits;
using AICentral.Logging.AzureMonitor.AzureMonitorLogging;
using AICentral.RateLimiting.DistributedRedis;
using AICentralWeb.QuickStartConfigs;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Logging.Console;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddSimpleConsole(options =>
{
    options.SingleLine = true;
});

if (builder.Environment.EnvironmentName != "tests")
{
    builder.Services
        .AddOpenTelemetry()
        .WithMetrics(metrics => { metrics.AddMeter(ActivitySource.AICentralTelemetryName); })
        .WithTracing(tracing =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // We want to view all traces in development
                tracing.SetSampler(new AlwaysOnSampler());
            }

            tracing.AddSource(ActivitySource.AICentralTelemetryName);
        })
        .UseAzureMonitor(options => options.SamplingRatio = 0.1f);
}

using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddSimpleConsole(options =>
{
    options.ColorBehavior = LoggerColorBehavior.Default;
    options.SingleLine = true;
}));
var startupLogger = loggerFactory.CreateLogger("AICentral.Startup");

builder.Services.AddCors();

if (builder.Environment.EnvironmentName == "APImProxyWithCosmosLogging")
{
    var config = new APImProxyWithCosmosLogging.Config();
    builder.Configuration.Bind("AICentral", config);
    var assembler = APImProxyWithCosmosLogging.BuildAssembler(config);

    assembler.AddServices(
        builder.Services,
        startupLogger: startupLogger,
        optionalHandler: null);
}
else
{
    builder.Services.AddAICentral(
        builder.Configuration,
        startupLogger: startupLogger,
        additionalComponentAssemblies:
        [
            typeof(AzureMonitorLoggerFactory).Assembly,
            typeof(PIIStrippingLogger).Assembly,
            typeof(DistributedRateLimiter).Assembly,
        ]);
}

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