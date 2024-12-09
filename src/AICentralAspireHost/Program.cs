using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var dapr = builder.AddDapr(options => { options.EnableTelemetry = true; })
    .AddDaprPubSub("aicentralpubsub");

var daprSubscriber = builder.AddProject<AICentral_Dapr_Audit>("aicentraldapraudit")
    .WithDaprSidecar()
    .WithReference(dapr)
    ;

var aicentral = builder.AddProject<AICentralWeb>("aicentralweb")
        .WithEnvironment("ASPNETCORE_Environment", "DaprAudit")
        .WithDaprSidecar()
        .WithReference(dapr)
    ;

builder.Build().Run();
