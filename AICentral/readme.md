# AI Central

AI Central gives you control over your AI services.

- Intelligent Routing
- Custom consumer OAuth2 authorisation
- Fallback AI service
- Round Robin AI services
- Lowest Latency AI service
- Circuit breakers, and backoff-retry over downstream AI services
- Request based and Token based rate limiting
- Prompt and usage logging
    - **Works for streaming endpoints as-well as non streaming**

## Configuration

See [docs on Github](https://github.com/microsoft/AICentral/) for more details.

## Minimal

This sample produces a AI-Central proxy that
- Listens on a hostname of your choosing
- Proxies directly through to a back-end Open AI server
- Can be accessed using standard SDKs

```json
{
  "AICentral": {
    "Endpoints": [
      {
        "Type": "AzureOpenAIEndpoint",
        "Name": "openai-1",
        "Properties": {
          "LanguageEndpoint": "https://<my-ai>.openai.azure.com"
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
    "Pipelines": [
      {
        "Name": "OpenAIPipeline",
        "Host": "mypipeline.mydomain.com",
        "EndpointSelector": "default"
      }
    ]
  }
}
```

## Full example

This pipeline will:

- Present an Azure Open AI, and an Open AI downstream as a single upstream endpoint
    - maps incoming Azure Open AI deployments to Open AI models
- Present it as an Azure Open AI style endpoint
- Protect the front-end by requiring an AAD token issued for your own AAD application
- Put a local Asp.Net core rate-limiting policy over the endpoint
- Add logging to Azure monitor
    - Logs quota, client caller information, and in this case the Prompt but not the response.

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
          "MaxConcurrency": 10
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
          "ClientId": "<my-client-id>",
          "TenantId": "<my-tenant-id>",
          "Instance": "https://login.microsoftonline.com/",
          "Audience": "<custom-audience>"
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
        "Type": "TokenBasedRateLimiting",
        "Name": "token-rate-limiter",
        "Properties": {
          "LimitType": "PerConsumer|PerAICentralEndpoint",
          "Window": 60,
          "PermitLimit": 1000
        }
      },
      {
        "Type": "AspNetCoreFixedWindowRateLimiting",
        "Name": "window-rate-limiter",
        "Properties": {
          "LimitType": "PerConsumer|PerAICentralEndpoint",
          "Options": {
            "Window": 10,
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
          "LogResponse": false
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
        ]
      }
    ]
  }
}

```
