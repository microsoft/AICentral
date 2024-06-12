var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var storageQueue = builder
    .AddAzureStorage("storage")
    .RunAsEmulator()
    .AddQueues("storage-queue");

builder.AddProject<Projects.AICentralExtensionsWeb>("aicentral")
    .WithReference(redis)
    .WithReference(storageQueue)
    .WithEnvironment("AICentral__GenericSteps__0__Properties__StorageQueueConnectionString", storageQueue)
    .WithEnvironment("AICentral__GenericSteps__1__Properties__RedisConfiguration", redis)
    ;

builder.Build().Run();