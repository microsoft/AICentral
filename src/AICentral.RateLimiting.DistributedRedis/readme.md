# AICentral.RateLimiting.DistributedRedis

A Distributed Rate Limiter for use with (AICentral)[www.github.com/microsoft/aicentral]

## Configuration

```shell

dotnet add package AICentral.RateLimiting.DistributedRedis

```

```csharp

builder.Services.AddAICentral(
    builder.Configuration,
    additionalComponentAssemblies:
    [
        typeof(DistributedRateLimiter).Assembly,
    ]);

```

```json

{
  "AICentral": {
    "GenericSteps": [
      {
        "Type": "DistributedRateLimiter",
        "Name": "request-rate-limiter",
        "Properties": {
          "LimitType": "PerEndpoint|PerConsumer",
          "MetricType": "Tokens|Requests",
          "Window": "00:00:10",
          "PermitLimit": 100,
          "RedisConfiguration" : "<Redis connection string>"
        }
      }
    ]    
  }
}

```

## How does it work?

For each pipeline a Redis Key is constructed which will identify this pipeline (optionally and the client) across AICentral Instances.
We store a Hash against this Redis key which is the current count of requests or tokens used.

Importantly the Hash Key contains a value derived from a Static point in time and the window information. This means the Key will always be the same across instances.

The Rate Limiter checks the sum of all the Hashes for the current window and if it is greater than the limit then the request is rejected.
The key automatically changes as we enter a new window meaning the totals are reset.
