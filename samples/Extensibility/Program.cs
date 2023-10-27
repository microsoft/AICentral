using AICentral.Configuration;
using AICentral.Pipelines;
using AICentral.Pipelines.Auth;
using AICentral.Pipelines.Endpoints;
using AICentral.Pipelines.EndpointSelectors.Random;
using AICentral.Pipelines.Logging;
using AICentral.Pipelines.RateLimiting;
using AICentral.Pipelines.Routes;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

builder.Services.AddAICentral(
    new AICentralOptions()
    {
        Pipelines = new List<AICentralPipeline>()
        {
            new AICentralPipeline(
                "ExtensibilitySample",
                new SimplePathMatchRouter("/deployments/model/completions"),
                new NoRateLimitingProvider(),
                new NoClientAuthAuthProvider(),
                new List<IAICentralPipelineStep>()
                {
                    new AzureMonitorLoggerPipelineStep("", "", true),
                },
                new RandomEndpointSelector(
                    new List<IAICentralEndpoint>()))
        },
        ExposeTestPage = true
    });

app.UseAICentral();

app.Run();