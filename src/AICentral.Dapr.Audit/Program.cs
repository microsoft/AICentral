using AICentral.Dapr.Audit;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

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

app.MapPost("aicentralaudit", async (
        [FromBody] LogEntry logEntry,
        [FromServices] DaprClient daprClient,
        [FromServices] ILogger<AICentralDaprAuditOptions> logger) =>
    {
        
        logger.LogDebug("Processing message from the queue");

        if (!options.PIIStrippingDisabled) {
            
            // var redacted = await textAnalyticsClient().RecognizePiiEntitiesBatchAsync(
            //     [loggingMessage.Prompt, loggingMessage.Response],
            //     cancellationToken: cancellationToken);
            //
            // //log the response
            // loggingMessage = loggingMessage with
            // {
            //     Prompt = string.IsNullOrWhiteSpace(loggingMessage.Prompt)
            //         ? string.Empty
            //         : redacted.Value[0].Entities.RedactedText,
            //     Response = string.IsNullOrWhiteSpace(loggingMessage.Response)
            //         ? string.Empty
            //         : redacted.Value[1].Entities.RedactedText
            // };
        }

        //save the message
        await daprClient.SaveStateAsync(
            options.StateStore,
            logEntry.id,
            logEntry
        );        
    })
    .WithTopic(options.PubSubName, options.TopicName);

app.UseCloudEvents();

app.Run();

