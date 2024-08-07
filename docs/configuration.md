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


| Property                | Description                                                                              |
|-------------------------|------------------------------------------------------------------------------------------|
| LanguageEndpoint        | Full URL to an Azure Open AI Endpoint                                                    |
| ModelMappings           | Maps incoming model names to backend model names.                                        |
| EnforceMappedModels     | If true, only models in the ModelMappings will be allowed.                               |
| AuthenticationType      | The type of authentication to use. ```apikey``` or ```entra``` or ```entrapassthrough``` |
| ApiKey                  | The key to use for authentication (when AuthenticationType is apikey).                   |
| MaxConcurrency          | The maximum number of concurrent requests to the endpoint.                               |
| AutoPopulateEmptyUserId | If true, the UserId will be populated with the incoming User Name if it is empty.        |

> If AuthenticationType is set to ```entra``` AICentral will use DefaultAzureCredential to obtain a JWT scoped to ```https://cognitiveservices.azure.com```

> If AuthenticationType is set to ```entrapassthrough``` AICentral will expect, and forward the incoming JWT Bearer Token straight through to Azure Open AI

> To provide a custom Authenticator, set AuthenticationType to the name of a 'BackendAuths' entry
 
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
        "EnforceMappedModels": true,
        "AuthenticationType": "ApiKey|Entra|EntraPassThrough",
        "ApiKey": "required-when-using-ApiKey",
        "MaxConcurrency": 5,
        "AutoPopulateEmptyUserId": true
    }
}
```

### Open AI Endpoint

| Property                | Description                                                                       |
|-------------------------|-----------------------------------------------------------------------------------|
| ModelMappings           | Maps incoming model names to backend model names.                                 |
| EnforceMappedModels     | If true, only models in the ModelMappings will be allowed.                        |
| ApiKey                  | Open AI API Key.                                                                  |
| MaxConcurrency          | The maximum number of concurrent requests to the endpoint.                        |
| AutoPopulateEmptyUserId | If true, the UserId will be populated with the incoming User Name if it is empty. |

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
        "MaxConcurrency": 5,
        "AutoPopulateEmptyUserId": true
    }
}
```

## Backend Auths

These allow more control over the downstream request to an Azure Open AI service.

We ship with one named 'BearerPlusKey' that enables a claim on an incoming JWT to be matched to an additional piece of data.

This enables out-of-the-box support for PromptFlow to use an Open AI JWT matched by AI Central, and propagated to Azure APIm along with a configured Subscription Key for the calling Identity.

APIm can then apply policy at a Product level, authorise the caller using the JWT, and then use its own Identity to call Azure Open AI.

> This requires an incoming Entra / JWT client auth step to work.

> This works because Entra will provide you a token for the Azure Open AI scope (https://cognitiveservices.azure.com) regardless if you have permissions to call any Azure Open AI resources. The authorisation check is made by Azure Open AI. Not Entra.

| Property          | Description                                             |
|-------------------|---------------------------------------------------------|
| IncomingClaimName | Claim to use to match against the SubjectToKeyMappings. |
| KeyHeaderName     | Header to attach to the call to the downstream service. |
| ClaimsToKeys      | Array mapping incoming identities to downstream keys.   |

```json
{
  "BackendAuths": [
    {
      "Type": "BearerPlusKey",
      "Name": "name-to-set-authentication-type-in-endpoint",
      "Properties": {
        "IncomingClaimName": "appid",
        "KeyHeaderName": "backend-api-key",
        "ClaimsToKeys": [
          {
            "ClaimValue": "app-1",
            "SubscriptionKey": "key-1"
          },
          {
            "ClaimValue": "app-2",
            "SubscriptionKey": "key-2"
          }
        ]
      }
    }
  ]
}
```



## Endpoint Selectors

Endpoint Selectors define clusters of Endpoints, along with logic for choosing which and when to use.

We ship 4 Endpoint Selectors:

### Single Endpoint Selector

- Direct proxy through to an existing endpoint

> This is the only endpoint selector for Azure Open AI that supports image generation. Azure Open AI uses an
> asynchronous poll to wait for image generation so we must guarantee affinity to an Azure Open AI service. 

| Property | Description                                                             |
|----------|-------------------------------------------------------------------------|
| Endpoint | An Endpoint name as declared in the Endpoint Configuration Collection . |

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

| Property  | Description                                                                       |
|-----------|-----------------------------------------------------------------------------------|
| Endpoints | An array of Endpoint names as declared in the Endpoint Configuration Collection . |

```json
{
  "Type": "RandomCluster",
  "Name": "my-name",
  "Properties": {
    "Endpoints": [
      "endpoint-name-from-earlier",
      "another-endpoint-name-from-earlier",
      "yet-another-endpoint-name-from-earlier",
      "or-another-endpoint-selector"
    ]
  }
}
```


### Highest Capacity Endpoint Selector

- Picks endpoints based on how much capacity (tokens, followed by requests) they advertise are remaining.
- If we fail, we pick the next.
- And so-on, until we get a response, or fail.

> This uses headers like ```x-ratelimit-remaining-requests``` and ```x-ratelimit-remaining-tokens``` to track available capacity.

***This selector cannot combine other Endpoint Selectors in its Endpoints Array***

| Property  | Description                                                                       |
|-----------|-----------------------------------------------------------------------------------|
| Endpoints | An array of Endpoint names as declared in the Endpoint Configuration Collection . |

```json
{
    "Type": "HighestCapacity",
    "Name": "my-name",
    "Properties": {
        "Endpoints": [
            "endpoint-name-from-earlier",
            "another-endpoint-name-from-earlier",
            "cannot-be-an-endpoint-selector-name"
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

| Property          | Description                                                                                     |
|-------------------|-------------------------------------------------------------------------------------------------|
| PriorityEndpoints | An array of Endpoint names as declared in the Endpoint Configuration Collection to try.         |
| FallbackEndpoints | An array of Endpoint names as declared in the Endpoint Configuration Collection to fallback to. |

```json
{
  "Type": "Prioritised",
  "Name": "my-name",
  "Properties": {
    "PriorityEndpoints": [
      "endpoint-name-from-earlier",
      "another-endpoint-name-from-earlier",
      "or-another-endpoint-selector"
    ],
    "FallbackEndpoints": [
      "yet-another-endpoint-name-from-earlier",
      "and-yet-another-endpoint-name-from-earlier",
      "or-another-endpoint-selector"
    ]
  }
}
```

### Lowest Latency Endpoint Selector

This runs a rolling average of the duration to call the downstream OpenAI endpoints. It will over time prioritise the fastest endpoints.
The implementation maintains the duration of the last 10 requests to an endpoint, and executes your request trying the quickest first.

> The strategy measures the overall response time. This works better when your Request and Response tokens are of a similar size.

| Property  | Description                                                                             |
|-----------|-----------------------------------------------------------------------------------------|
| Endpoints | An array of Endpoint names as declared in the Endpoint Configuration Collection to try. |

```json
{
    "Type": "LowestLatency",
    "Name": "my-name",
    "Properties": {
        "Endpoints": [
            "endpoint-name-from-earlier",
            "another-endpoint-name-from-earlier",
            "or-another-endpoint-selector"
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

## Pipeline Configuration

| Property                             | Required | Description                                                                               |
|--------------------------------------|----------|-------------------------------------------------------------------------------------------|
| Name                                 | Yes      | Friendly Name of the pipeline                                                             |
| Host                                 | Yes      | The HostName to listen to for incoming requests to this pipeline                          |
| EndpointSelector                     | Yes      | The Endpoint Selector strategy to use as defined in your EndpointSelectors config section |
| AuthProvider                         | Yes      | Auth strategy to protect the Pipeline, as defined in your AuthProviders config section    |
| OpenTelemetryConfig.Transmit         | Yes      | True to emit additional Open Telemetry metrics (useful for scenarios such as ChargeBack)  |
| OpenTelemetryConfig.AddClientNameTag | Yes      | True to add the Client Name tag to OTel telemetry                                         |
| OpenTelemetryConfig.AddClientNameTag | Yes      | True to add the Client Name tag to OTel telemetry                                         |
| Steps                                | No       | An array of Step names to run before the request is forwarded to the backend.             |

```json
{
    "Name": "MyPipeline",
    "Host": "<host-name-we-listen-for-requests-on>",
    "EndpointSelector": "name-from-above",
    "AuthProvider": "name-from-above",
    "OpenTelemetryConfig": {
        "Transmit": true,
        "AddClientNameTag": true
    },
    "Steps": [
        "step-name-from-earlier",
        "another-step-name-from-earlier"
    ]
}
```

## Minimal Pipeline configuration

Using Endpoints and Endpoint Selectors we can create a pipeline like this:

```json
{
    "AICentral": {
        "Endpoints": [ "... as above" ],
        "EndpointSelectors": [ "... as above" ],
        "Pipelines": [
            {
                "Name": "MyPipeline",
                "Host": "<host-name-we-listen-for-requests-on>",
                "EndpointSelector": "name-from-EndpointSelectors-config-section",
                "AuthProvider": "name-from-AuthProviders-config-section"
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

| Property | Required | Description                               |
|----------|----------|-------------------------------------------|
| Name     | Yes      | Name to refer to the step from a Pipeline |
| Type     | Yes      | AllowAnonymous                            |


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

| Property           | Required | Description                                                                                                    |
|--------------------|----------|----------------------------------------------------------------------------------------------------------------|
| Name               | Yes      | Name to refer to the step from a Pipeline                                                                      |
| Type               | Yes      | Entra                                                                                                          |
| Entra.xxx          | Yes      | Standard [Microsoft.Identity.Web](https://www.nuget.org/packages/Microsoft.Identity.Web) Configuration section |
| Requirements.Roles | No       | Role claim to assert on the incoming validated JWT                                                             |

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

You can specify clients, along with a pair of keys, and authenticate your pipelines using them. The keys are sent in the api-key header and replace the provider's key.

| Property          | Required | Description                                  |
|-------------------|----------|----------------------------------------------|
| Name              | Yes      | Name to refer to the step from a Pipeline    |
| Type              | Yes      | ApiKey                                       |
| Clients           | Yes      | Array of allowed Clients                     |
| Client.ClientName | Yes      | Name to assign to the incoming callee        |
| Client.Key1       | Yes      | ApiKey valid for the consumer to pass        |
| Client.Key2       | Yes      | Second ApiKey valid for the consumer to pass |


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

### Inbuilt JWT Token Provider

AI Central can act as a Token Provider. The tokens are bound to a Consumer, Pipelines, and a time window. 

> Use this to facilitate a Hackathon without blowing your budget!

| Property       | Required | Description                                                                                                   |
|----------------|----------|---------------------------------------------------------------------------------------------------------------|
| Name           | Yes      | Name to refer to the step from a Pipeline                                                                     |
| Type           | Yes      | AICentralJWT                                                                                                  |
| TokenIssuer    | Yes      | Issuer to set / require on JWTs                                                                               |
| AdminKey       | Yes      | A secret that can be provided to create JWTs                                                                  |
| ValidPipelines | Yes      | Dictionary of Pipeline Names the token is valid for, with the Deployments it is valid for (can be a wildcard) |

```json
{
  "AICentral": {
    "AuthProviders": [
      {
        "Type": "AICentralJWT",
        "Name": "hackathon",
        "Properties": {
          "TokenIssuer": "https://hackathon.auth.graeme.com",
          "AdminKey": "<hard-to-guess-api-key>",
          "ValidPipelines": {
            "MyPipeline": ["Deployment1", "Deployment2"],
            "MyPipeline2": ["*"]
          }
        }
      }
    ],
    "Endpoints": [
      {
        "Name": "MyPipeline",
        "Host": "<host-name-we-listen-for-requests-on>",
        "EndpointSelector": "name-from-above",
        "AuthProvider": "hackathon"
      },
      {
        "Name": "MyPipeline2",
        "Host": "<host-name-we-listen-for-requests-on>",
        "EndpointSelector": "name-from-above",
        "AuthProvider": "hackathon-2"
      }
    ]
  }
}
```

```bash
# The above pipeline will expose an endpoint that can mint JWT's
curl -X POST https://<host-name-we-listen-for-requests-on>/aicentraljwt/<auth-provider-name>/tokens \
     -H "api-key=<hard-to-guess-api-key>" \
     -d "{ \"names\": [\"Consumer-1\", \"Consumer-2\", ...], \"ValidPipelines\": [\"MyPipeline\", ...], \"ValidFor\": \"00:24:00\" }"
```


## "Steps"

A pipeline can run multiple steps. We currently provide steps for:

- Azure Monitor Logging
- Asp.Net Core Windowed Rate Limiting
- Token Based Rate Limiting

### Token and call based rate limiting

| Property            | Required | Description                                                                                |
|---------------------|----------|--------------------------------------------------------------------------------------------|
| Name                | Yes      | Name to refer to the step from a Pipeline                                                  |
| Type                | Yes      | TokenBasedRateLimiting                                                                     |
| LimitType           | Yes      | PerConsumer to limit to each Consumer. PerAICentralEndpoint to protect the entire Endpoint |
| MetricType          | Yes      | Tokens or Requests.                                                                        |
| Options.Window      | Yes      | How long to count for before resetting the counter                                         |
| Options.PermitLimit | Yes      | How high to let the counter go before returning 429's to the Consumer                      |

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


| Property    | Required | Description                               |
|-------------|----------|-------------------------------------------|
| Name        | Yes      | Name to refer to the step from a Pipeline |
| Type        | Yes      | TokenBasedRateLimiting                    |
| WorkspaceId | Yes      | Id of the Workspace from Azure            |
| Key         | Yes      | Key to post data to the Workspace.        |
| LogPrompt   | Yes      | True to log the text from the prompt      |
| LogResponse | Yes      | True to log the text from the response    |

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
