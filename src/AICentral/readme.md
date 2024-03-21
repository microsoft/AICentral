# AI Central

AI Central gives you control over your AI services.

- Lightweight out-the-box token logging using Open Telemetry
- Intelligent Routing
- Custom consumer OAuth2 authorisation
- Fallback AI service
- Round Robin AI services
- Lowest Latency AI service
- Circuit breakers, and backoff-retry over downstream AI services
- Request based and Token based rate limiting
- Prompt and usage logging
    - **Works for streaming endpoints as-well as non streaming**
- Additional Open Telemetry Metrics and Traces

## Configuration

See [docs on Github](https://github.com/microsoft/AICentral/) for more details.

## Minimal

This sample produces a AI-Central proxy that
- Listens on a hostname of your choosing
- Proxies directly through to a back-end Open AI server
- Can be accessed using standard SDKs
- Outputs open-telemetry metrics to capture usage information

```json
{
  "AICentral": {
    "Endpoints": [
      {
        "Type": "AzureOpenAIEndpoint",
        "Name": "openai-1",
        "Properties": {
          "LanguageEndpoint": "https://<my-ai>.openai.azure.com",
          "AuthenticationType": "ApiKey",
          "ApiKey": "<key>"
        }
      }
    ],
    "EndpointSelectors": [
      {
        "Type": "SingleEndpoint",
        "Name": "default",
        "Properties": {
          "Endpoint": "openai-1"
        }
      }
    ],
    "AuthProviders": [
      {
        "Type": "ApiKey",
        "Name": "apikey",
        "Properties": {
          "Clients": [
            {
              "ClientName": "Consumer-1",
              "Key1": "<random-key>",
              "Key2": "<random-key>"
            }
          ]
        }
      }
    ],
    "Pipelines": [
      {
        "Name": "OpenAIPipeline",
        "Host": "mypipeline.mydomain.com",
        "EndpointSelector": "default",
        "AuthProvider": "apikey",
        "OpenTelemetryConfig": {
          "AddClientNameTag": true,
          "Transmit": true
        }
      }
    ]
  }
}
```

## Full example

This pipeline will:

- Present an Azure Open AI, and an Open AI downstream as a single upstream endpoint
  - maps the incoming deployment Name "GPT35Turbo0613" to the downstream Azure Open AI deployment "MyGptModel"
  - maps incoming Azure Open AI deployments to Open AI models
- Present it as an Azure Open AI style endpoint
- Protect the front-end by requiring an AAD token issued for your own AAD application
- Put a local Asp.Net core rate-limiting policy over the endpoint
- Emit Open Telemetry to be picked up by your OTel collector
- Add rich logging to Azure monitor
    - Logs quota, client caller information (IP and identity name), and in this case the Prompt but not the response.
- Publish the client-name as a tag in Open Telemetry

```json
{
  "AICentral": {
    "Endpoints": [
      {
        "Type": "AzureOpenAIEndpoint",
        "Name": "openai-priority",
        "Properties": {
          "LanguageEndpoint": "https://<my-ai>.openai.azure.com",
          "AuthenticationType": "Entra|EntraPassThrough|ApiKey",
          "MaxConcurrency": 10,
          "ModelMappings": {
            "Gpt35Turbo0613": "MyGptModel"
          }
        }
      },
      {
        "Type": "OpenAIEndpoint",
        "Name": "openai-fallback",
        "Properties": {
          "LanguageEndpoint": "https://api.openai.com",
          "ModelMappings": {
            "Gpt35Turbo0613": "gpt-3.5-turbo",
            "Ada002Embedding": "text-embedding-ada-002"
          },
          "ApiKey": "<my-api-key>",
          "Organization": "<optional-organisation>"
        }
      }
    ],
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
          }
        }
      }
    ],
    "EndpointSelectors": [
      {
        "Type": "Prioritised",
        "Name": "my-endpoint-selector",
        "Properties": {
          "PriorityEndpoints": ["openai-1"],
          "FallbackEndpoints": ["openai-fallback"]
        }
      }
    ],
    "GenericSteps": [
      {
        "Type": "AspNetCoreFixedWindowRateLimiting",
        "Name": "token-rate-limiter",
        "Properties": {
          "LimitType": "PerConsumer|PerAICentralEndpoint",
          "MetricType": "Tokens",
          "Options": {
            "Window": "00:01:00",
            "PermitLimit": 1000
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
      },
      {
        "Type": "AzureMonitorLogger",
        "Name": "azure-monitor-logger",
        "Properties": {
          "WorkspaceId": "<workspace-id>",
          "Key": "<key>",
          "LogPrompt": true,
          "LogResponse": false,
          "LogClient": true
        }
      },
      {
        "Type": "BulkHead",
        "Name": "bulk-head",
        "Properties": {
          "MaxConcurrency": 20
        }
      }
    ],
    "Pipelines": [
      {
        "Name": "MyPipeline",
        "Host": "prioritypipeline.mydomain.com",
        "EndpointSelector": "my-endpoint-selector",
        "AuthProvider": "simple-aad",
        "Steps": [
          "window-rate-limiter",
          "bulk-head",
          "azure-monitor-logger"
        ],
        "OpenTelemetryConfig": {
          "AddClientNameTag": true,
          "Transmit": true
        }
      }
    ]
  }
}

```

