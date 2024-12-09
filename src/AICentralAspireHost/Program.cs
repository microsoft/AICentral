using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var dapr = builder.AddDapr(options => { options.EnableTelemetry = true; });
var daprState = dapr.AddDaprStateStore("aicentralstate");
var daprPubSub = dapr.AddDaprPubSub("aicentralpubsub");

var daprSubscriber = builder.AddProject<AICentral_Dapr_Audit>("aicentraldapraudit")
        .WithDaprSidecar()
        .WithReference(daprPubSub)
        .WithReference(daprState);

var aicentral = builder.AddProject<AICentralWeb>("aicentralweb")
        .WithEnvironment("ASPNETCORE_Environment", "DaprAudit")
        .WithDaprSidecar()
        .WithReference(daprPubSub);

builder.Build().Run();