using AICentral.Configuration;
using AICentral.Pipelines;
using AICentral.Pipelines.Auth;
using AICentral.Pipelines.Endpoints;
using AICentral.Pipelines.EndpointSelectors.Random;
using AICentral.Pipelines.Routes;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// builder.Services.AddAICentral(
//     new AICentralOptions(new List<AICentralPipeline>()
//         {
//             new AICentralPipeline(
//                 "ExtensibilitySample",
//                 new SimplePathMatchRouter("/deployments/model/completions"),
//                 new NoClientAuthAuthRuntime(),
//                 new List<IAICentralPipelineStepRuntime>(),
//                 new RandomEndpointSelectorRuntime(Array.Empty<IAICentralEndpointRuntime>()))
//         }
//     });
//
// app.UseAICentral();

app.Run();