# AICentral Configuration

Pipelines are configured from pre-defined components, each of which can be declared as configuration.

All pipelines require the following as a minimum:

## Endpoints

Defines the backend target server. Current supported endpoints are Azure Open AI and Open AI.

All endpoints are wrapped with a Polly Policy. We
 - Retry on 429 errors
 - Circuit break if an endpoint consistently fails
 - Will setup a BulkHead to limit concurrency to the endpoint (leave off the MaxConcurrency if you don't want this)

### Azure Open AI Endpoint

```json
{
    "Type": "AzureOpenAIEndpoint",
    "Name": "name-to-refer-to-later",
    "Properties": {
        "LanguageEndpoint": "required-full-uri-to-azure-open-ai-service",
        "ModelMappings": {
            "incoming-model-name": "backend-model-name",
            "not-required": "default-to-pass-model-name-through"
        },
        "AuthenticationType": "ApiKey|Entra|EntraPassThrough",
        "AuthenticationKey": "required-when-using-ApiKey",
        "MaxConcurrency": 5
    }
}
```

### Open AI Endpoint

```json
{
    "Type": "OpenAIEndpoint",
    "Name": "name-to-refer-to-later",
    "Properties": {
        "ModelMappings": {
            "incoming-model-name": "backend-model-name",
            "not-required": "default-to-pass-model-name-through"
        },
        "ApiKey": "required",
        "Organization": "optional",
        "MaxConcurrency": 5
    }
}
```

## Endpoint Selectors

Endpoint Selectors define clusters of Endpoints, along with logic for choosing which and when to use.

We ship 4 Endpoint Selectors:

### Single Endpoint Selector

- Direct proxy through to an existing endpoint

> This is the only endpoint selector for Azure Open AI that supports image generation. Azure Open AI uses an
> asynchronous poll to wait for image generation so we must guarantee affinity to an Azure Open AI service. 

```json
{
    "Type": "SingleEndpoint",
    "Name": "my-name",
    "Properties": {
        "Endpoint": "endpoint-name-from-earlier"
    }
}
```

### Random Endpoint Selector

- Picks an endpoint at random and tries it.
- If we fail, we pick from the remaining ones.
- And so-on, until we get a response, or fail.

```json
{
    "Type": "RandomCluster",
    "Name": "my-name",
    "Properties": {
        "Endpoints": [
            "endpoint-name-from-earlier",
            "another-endpoint-name-from-earlier",
            "yet-another-endpoint-name-from-earlier"
          ]
    }
}
```

### Prioritised Endpoint Selector

- For the Priority services
  - Picks an endpoint at random and tries it.
  - If we fail, we pick from the remaining ones.
  - And so-on, until we get a response, or fail.
- If we failed, repeat for the fallback services

```json
{
  "Type": "Prioritised",
  "Name": "my-name",
  "Properties": {
    "PriorityEndpoints": [
      "endpoint-name-from-earlier",
      "another-endpoint-name-from-earlier"
    ],
    "FallbackEndpoints": [
      "yet-another-endpoint-name-from-earlier",
      "and-yet-another-endpoint-name-from-earlier"
    ]
  }
}
```

### Lowest Latency Endpoint Selector

This runs a rolling average of the duration to call the downstream OpenAI endpoints. It will over time prioritise the fastest endpoints.
The implementation maintains the duration of the last 10 requests to an endpoint, and executes your request trying the quickest first.

```json
{
    "Type": "LowestLatency",
    "Name": "my-name",
    "Properties": {
        "Endpoints": [
            "endpoint-name-from-earlier",
            "another-endpoint-name-from-earlier"
          ]
    }
}
```

## Referencing Endpoint Selectors from Endpoint Selectors

To support more complex Endpoint Selectors we support referencing an Endpoint Selector from another Endpoint Selector.

The implementation relies on the order of your Selectors. You can only reference selectors that have been defined earlier. This sample shows a Lowest Latency endpoint used for the priority endpoints in a Prioritised endpoint selector.

```json5
{
  "AICentral": {
    "Endpoints": [ "... define endpoints" ],
    "EndpointSelectors": [
      {
        "Type": "LowestLatency",
        "Name": "lowest-latency-group",
        "Properties": {
          "Endpoints": [
            "endpoint-name-from-earlier",
            "another-endpoint-name-from-earlier"
          ]
        }
      },
      {
        "Type": "Prioritised",
        "Properties": {
          "PriorityEndpoints": [
            "lowest-latency-group" //references the lowest-latency-group defined before this
          ],
          "FallbackEndpoints": [
            "yet-another-endpoint-name-from-earlier",
            "and-yet-another-endpoint-name-from-earlier"
          ]
        }
      },
      {
        
      }
    ],
    "Pipelines": [
      {
        "Name": "MyPipeline",
        "Host": "<host-name-we-listen-for-requests-on>",
        "EndpointSelector": "name-from-above"
      }
    ]
  }
}
```

## Minimal Pipeline configuration

Using Endpoints and Endpoint Selectors we can create a pipeline like this:

> Pipelines can detect the incoming service type using classes that implement ```IAIServiceDetector```. We support Azure Open AI, and Open AI endpoints. You can register your own implementation to support other AI services.

```json
{
    "AICentral": {
        "Endpoints": [ "... as above" ],
        "EndpointSelectors": [ "... as above" ],
        "Pipelines": [
            {
                "Name": "MyPipeline",
                "Host": "<host-name-we-listen-for-requests-on>",
                "EndpointSelector": "name-from-above"
            }
        ]
    }
}
```
## Open Telemetry

To enable OTel metrics on a pipeline, add this section

> AddClientNameTag adds the consumers name to the OTel metrics. This will enable chargeback scenarios across your Pipelines.

> The examples shown will capture Telemetry and send it to Azure Monitor. Use your Open Telemetry collector of choice for other destinations. 

```json
{
  "AICentral": {
    "Endpoints": [ "... as above" ],
    "EndpointSelectors": [ "... as above" ],
    "Pipelines": [
      {
        "Name": "MyPipeline",
        "Host": "<host-name-we-listen-for-requests-on>",
        "EndpointSelector": "name-from-above",
        "OpenTelemetryConfig": {
          "AddClientNameTag": true,
          "Transmit": true
        }
      }
    ]
  }
}
```

```bash
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore
```

```csharp
    builder.Services
        .AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics.AddMeter(ActivitySource.AICentralTelemetryName);
        })
        .UseAzureMonitor();

```

> Check out this [dashboard](../infra/dashboards/aicentral-dashboards.json) for inspiration on how to visualise your metrics.


To enable additional AICentrl traces in your Open Telemetry distributed tracing
```csharp
    builder.Services
        .AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing.AddSource(ActivitySource.AICentralTelemetryName);
        })

```

## Incoming Client Auth

We support adding authentication to incoming clients in 3 ways.

### Anonymous

No auth is applied to incoming requests. This is useful if we use EntraPassThrough for our backend endpoints. The user will present a token issued for an Azure Open AI service, which will be accepted or rejected by the backend service.

```json
{
  "AICentral": {
    "AuthProviders": [
      {
        "Type": "AllowAnonymous",
        "Name": "no-auth"
      }
    ],
    "Endpoints": [
      {
        "Name": "MyPipeline",
        "Host": "<host-name-we-listen-for-requests-on>",
        "EndpointSelector": "name-from-above",
        "AuthProvider": "no-auth"
      }
    ]
  }
}
```

### Entra

Uses standard Azure Active Directory Authentication to assert a valid JWT.

> Currently we support authorisation using AAD Roles.

```json
{
  "AICentral": {
    "AuthProviders": [
      {
        "Type": "Entra",
        "Name": "simple-aad",
        "Properties": {
          "Entra": {
            "ClientId": "<my-client-id>",
            "TenantId": "<my-tenant-id>",
            "Instance": "https://login.microsoftonline.com/",
            "Audience": "<custom-audience>"
          },
          "Requirements" : {
            "Roles": ["required-roles", "can-be-many"]
          }
        }
      }
    ],
    "Endpoints": [
      {
        "Name": "MyPipeline",
        "Host": "<host-name-we-listen-for-requests-on>",
        "EndpointSelector": "name-from-above",
        "AuthProvider": "simple-aad"
      }
    ]
  }
}
```

### Client-Keys

You can specify clients, along with a pair of keys, and authenticate your pipelines using them.

```json
{
  "AICentral": {
    "AuthProviders": [
      {
        "Type": "ApiKey",
        "Name": "apikey",
        "Properties": {
          "Clients" : [
            {
              "ClientName" : "Consumer-1",
              "Key1": "dfhaskjhdfjkasdhfkjsdf",
              "Key2": "23sfdkjhcijshjkfhsdkjfsd"
            },
            {
              "ClientName" : "Consumer-2",
              "Key1": "szcvjhkhkjhjkfsdf",
              "Key2": "vkjhsdfjkhkjnkjhjksdf"
            }
          ]
        }
      }
    ],
    "Endpoints": [
      {
        "Name": "MyPipeline",
        "Host": "<host-name-we-listen-for-requests-on>",
        "EndpointSelector": "name-from-above",
        "AuthProvider": "apikey"
      }
    ]
  }
}
```

## "Steps"

A pipeline can run multiple steps. We currently provide steps for:

- Azure Monitor Logging
- Asp.Net Core Windowed Rate Limiting
- Token Based Rate Limiting

### Token and call based rate limiting

```json
{
  "AICentral": {
    "GenericSteps": [
      {
        "Type": "TokenBasedRateLimiting",
        "Name": "token-rate-limiter",
        "Properties": {
          "LimitType": "PerConsumer|PerAICentralEndpoint",
          "MetricType": "Tokens",
          "Options": {
            "Window": "00:00:10",
            "PermitLimit": 100
          }
        }
      },
      {
        "Type": "AspNetCoreFixedWindowRateLimiting",
        "Name": "window-rate-limiter",
        "Properties": {
          "LimitType": "PerConsumer|PerAICentralEndpoint",
          "MetricType": "Requests",
          "Options": {
            "Window": "00:00:10",
            "PermitLimit": 100
          }
        }
      }
    ],
    "Endpoints": [
      {
        "Name": "MyPipeline",
        "Host": "<host-name-we-listen-for-requests-on>",
        "Steps": [
          "token-rate-limiter",
          "window-rate-limiter"
        ]
      }
    ]
  }
}


```

### Azure Monitor logging

> Requires the AICentral.Extensions.AzureMonitor package

``` dotnet package add AICentral.Extensions.AzureMonitor package ```

```csharp

builder.Services.AddAICentral(
    builder.Configuration,
    startupLogger: new SerilogLoggerProvider(logger).CreateLogger("AICentralStartup"),
    additionalComponentAssemblies:
    [
        typeof(AzureMonitorLoggerFactory).Assembly //AI Central Azure Monitor extension assembly  
    ]);
```

```json
{
  "AICentral": {
    "GenericSteps": [
      {
        "Type": "AzureMonitorLogger",
        "Name": "azure-monitor-logger",
        "Properties": {
          "WorkspaceId": "<workspace-id>",
          "Key": "<key>>",
          "LogPrompt": true,
          "LogResponse": true
        }
      }
    ],
    "Endpoints": [
      {
        "Name": "MyPipeline",
        "Host": "<host-name-we-listen-for-requests-on>",
        "Steps": [
          "azure-monitor-logger"
        ]
      }
    ]
  }
}


```

# Customisation

AI Central is extensible. You can bring your own implementations of Steps, Endpoints, Endpoint Selectors, Auth Providers. The only thing we specify is a pipeline must:

- Trigger based on an incoming Host Header
- Choose an Endpoint Selector

We default certain properties if you don't provide them.

- We default to expecting Azure Open AI type requests
- We default to Entra Pass Through auth for Azure Open AI backends
- We default to Anonymous Auth (we don't validate tokens for Azure Open AI)

TODO; We are working on adding an extensibility sample.
