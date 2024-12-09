using AICentral.Dapr.Audit;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.EnvironmentName != "tests" && Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") != null)
{
    builder.Services
        .AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // We want to view all traces in development
                tracing.SetSampler(new AlwaysOnSampler());
            }
        })
        .UseAzureMonitor(options => options.SamplingRatio = 0.1f);
}

if (builder.Environment.EnvironmentName != "tests" &&
    Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") != null)
{
    builder.Services
        .AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // We want to view all traces in development
                tracing.SetSampler(new AlwaysOnSampler());
            }
        })
        .UseOtlpExporter();
}

var options = new AICentralDaprAuditOptions();
builder.Configuration.Bind("AICentralDaprAudit", options);
builder.Services.AddOptions<AICentralDaprAuditOptions>("AICentralDaprAudit");

var app = builder.Build();

app.MapSubscribeHandler();

app.MapPost("aicentralaudit", async (HttpContext context, [FromBody] LogEntry logEntry,
        [FromServices] ILogger<AICentralDaprAuditOptions> logger) =>
    {
        logger.LogInformation("Received audit message: {PubSubName}, {TopicName}", options.PubSubName,
            options.TopicName);
    })
    .WithTopic(options.PubSubName, options.TopicName);

app.UseCloudEvents();

app.Run();

