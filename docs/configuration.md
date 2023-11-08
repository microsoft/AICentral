# AICentral Configuration

Pipelines are configured from pre-defined components, each of which can be declared as configuration.

All pipelines require the following as a minimum:

## Endpoints

Defines the backend target server. Current supported endpoints are Azure Open AI and Open AI.

All endpoints are wrapped with a Polly Policy. We
 - Retry on 429 errors
 - Circuit break if an endpoint consistently fails

### Azure Open AI Endpoint

```
{
    "Type": "AzureOpenAIEndpoint",
    "Name": "name-to-refer-to-later",
    "Properties" {
        "LanguageEndpoint": "required-full-uri-to-azure-open-ai-service",
        "ModelMappings" {
            "incoming-model-name": "backend-model-name",
            "not-required": "default-to-pass-model-name-through"
        },
        "AuthenticationType": "ApiKey|Entra|EntraPassThrough",
        "AuthenticationKey": "required-when-using-ApiKey"
    }
}
```

### Open AI Endpoint

```
{
    "Type": "OpenAIEndpoint",
    "Name": "name-to-refer-to-later",
    "Properties" {
        "ModelMappings" {
            "incoming-model-name": "backend-model-name",
            "not-required": "default-to-pass-model-name-through"
        },
        "ApiKey": "required",
        "Organization": "optional",
    }
}
```

## Endpoint Selectors

Endpoint Selectors define clusters of Endpoints, along with logic for choosing which and when to use.

We ship 3 Endpoint Selectors:

### Single Endpoint Selector

- Direct proxy through to an existing endpoint

> This is the only endpoint selector for Azure Open AI that supports image generation. Azure Open AI uses an
> asynchronous poll to wait for image generation so we must guarantee affinity to an Azure Open AI service. 

```
{
    "Type": "SingleEndpoint",
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
                "EndpointSelector": "name-from-above"
            }
        ]
    }
}
```

If we want the pipeline to be exposed as an Open AI Pipeline, not an Azure Open AI Pipeline we can set the EndpointType flag.

This changes the way we interpret the different incoming URLs, and where we look for the model name.

```json
{
    "AICentral": {
        "Endpoints": [ "... as above" ],
        "EndpointSelectors": [ "... as above" ],
        "Pipelines": [
            {
                "Name": "MyPipeline",
                "Host": "<host-name-we-listen-for-requests-on>",
                "EndpointType": "OpenAI",
                "EndpointSelector": "name-from-above"
            }
        ]
    }
}
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
> We don't perform any role checks, so make sure you only allow issuance of tokens to clients you want.

```json
{
  "AICentral": {
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

### Azure Monitor Logger

```json
{
  "AICentral": {
    "GenericSteps": [
      {
        "Type": "AspNetCoreFixedWindowRateLimiting",
        "Name": "window-rate-limiter",
        "Properties": {
          "Window": 10,
          "PermitLimit": 100
        }
      },
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
          "window-rate-limiter",
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
