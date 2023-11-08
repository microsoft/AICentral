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
            "another-endpoint-name-from-earlier",
          ],
        "FallbackEndpoints": [
            "yet-another-endpoint-name-from-earlier",
            "and-yet-another-endpoint-name-from-earlier",
          ],
    }
}
```

# Minimal Pipeline configuration

Using Endpoints and Endpoint Selectors we can create a pipeline like this:

```json
{
    "AICentral": {
        "Endpoints": [ ... as above ],
        "EndpointSelectors": [ ... as above ],
        "Pipelines": [
            {
                "Name": "MyPipeline",
                "Host": "<host-name-we-listen-for-requests-on>",
                "EndpointSelector": "name-from-above",
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
