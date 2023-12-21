# AI Central

![main](https://github.com/microsoft/aicentral/actions/workflows/build.yaml/badge.svg)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/mit)
[![NuGet Status](http://img.shields.io/nuget/v/aicentral.svg?style=flat)](https://www.nuget.org/packages/aicentral/)

AI Central gives you control over your AI services.

- Intelligent Routing
  - Random endpoint selector
  - Prioritised endpoint selector with fallback
  - Lowest Latency endpoint selector
- Can proxy asynchronous requests such as Azure Open AI Image Generation across fleets of servers
- Custom consumer OAuth2 authorisation
- Circuit breakers, and backoff-retry over downstream AI services
- Local token rate limiting
  - By consumer / by endpoint
  - By number of tokens (including streaming by estimated token count)
- Local request rate limiting
  - By consumer / by endpoint
- Bulkhead support for buffering requests to backend
- Prompt and usage logging to Azure Monitor
  - **Works for streaming endpoints as-well as non streaming**

> Extensibility model makes it easy to build your own plugins

## Configuration

See [Configuration](./docs/configuration.md) for more details.

## Minimal

This sample produces a AI-Central proxy that
 - Listens on a hostname of your choosing
 - Proxies directly through to a back-end Open AI server
 - Can be accessed using standard SDKs

### Installation

#### Docker

```bash
# Run container in Docker referencing a local configuration file
docker run -p 8080:80 -v .\appsettings.Development.json:/app/appsettings.Development.json -e ASPNETCORE_ENVIRONMENT=Development ghcr.io/graemefoster/aicentral:latest
```

#### Asp.Net CORE 

```bash
#Create new project and bootstrap the AICentral nuget package
dotnet new web -o MyAICentral
cd MyAICentral
dotnet add package AiCentral
```
#### Program.cs
```csharp
//Minimal API to configure AI Central
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAICentral(builder.Configuration);

var app = builder.Build();

app.UseAICentral();

app.Run();


```

### appsettings.&lt;environment&gt;.json
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
    "AuthProviders": [
      {
        "Type": "AllowAnonymous",
        "Name": "allow-anonymous-auth"
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
        "Name": "AzureOpenAIPipeline",
        "Host": "mypipeline.mydomain.com",
        "AuthProvider": "allow-anonymous-auth",
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
          "AuthenticationType": "Entra|EntraPassThrough|ApiKey"
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
          "LogResponse": false
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
          "azure-monitor-logger"
        ]
      }
    ]
  }
}

```

