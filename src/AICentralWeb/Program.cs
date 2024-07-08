using AICentral;
using AICentral.Configuration;
using AICentral.DistributedTokenLimits;
using AICentral.Logging.AzureMonitor.AzureMonitorLogging;
using AICentral.RateLimiting.DistributedRedis;
using AICentralWeb.QuickStartConfigs;
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

var logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo
    .Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry().AddSerilog(logger);

builder.Services.AddCors();

if (builder.Environment.EnvironmentName == "APImProxyWithCosmosLogging")
{
    var config = new APImProxyWithCosmosLogging.Config();
    builder.Configuration.Bind("AICentral", config);
    var assembler = APImProxyWithCosmosLogging.BuildAssembler(config);

    assembler.AddServices(
        builder.Services,
        startupLogger: new SerilogLoggerProvider(logger).CreateLogger("AICentralStartup"),
        optionalHandler: null);
}
else
{
    builder.Services.AddAICentral(
        builder.Configuration,
        startupLogger: new SerilogLoggerProvider(logger).CreateLogger("AICentralStartup"),
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