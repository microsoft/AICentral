# AICentral.AzureAISearchVectorizer

A customer vectorizer endpoint enabling Azure AI Search to calculate embeddings using end to end private endpoints, in an architecture leveraging Azure API Management's AI Gateway.

## Configuration

```shell

dotnet add package AICentral.AzureAISearchVectorizer

```

```csharp

builder.Services.AddAICentral(
    builder.Configuration,
    additionalComponentAssemblies:
    [
        typeof(AzureAISearchVectorizerProxy).Assembly,
    ]);

```

```json

{
  "AICentral": {
    "RouteProxies": [
      {
        "Type": "AzureAISearchVectorizerProxy",
        "Name": "azureAISearchVectorizer",
        "Properties": {
          "EmbeddingsDeploymentName": "embeddings",
          "ProxyPath": "/aisearchembeddings",
          "OpenAIApiVersion": "2024-04-01-preview"
        }
      }
    ],
    "Pipelines": [
      {
        "Name": "gpt4o",
        "Host": "*",
        "EndpointSelector": "endpoint",
        "AuthProvider": "anonymous",
        "RouteProxies": ["azureAISearchVectorizer"],
        "Steps": [
        ]
      }
    ]
  }
}

```

## How does it work?

The middleware exposes a new AI Central Endpoint for a pipeline at the '/ProxyPath' URL, authorised the same was as the rest of the pipeline.
This endpoint can be provided to an Azure AI Search instance as a custom vectorizer endpoint to calculate embeddings on AI Search requests.

