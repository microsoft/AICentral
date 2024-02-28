using AICentral.Configuration;

var builder = WebApplication.CreateBuilder(args);

// builder.Services
//     .AddOpenTelemetry()
//     .WithMetrics(metrics =>
//     {
//         metrics.AddMeter(ActivitySource.AICentralTelemetryName);
//     })
//     .WithTracing(tracing =>
//     {
//         tracing.AddSource(ActivitySource.AICentralTelemetryName);
//     })
//     .UseAzureMonitor();

builder.Services.AddAICentral(
    builder.Configuration,
    additionalComponentAssemblies: new []
    {
        typeof(Program).Assembly
    });

var app = builder.Build();

app.UseAICentral();

app.Run();