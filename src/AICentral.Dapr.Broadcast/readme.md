# DAPR Broadcast for AI Central

Enables AI Central events to be broadcast via DAPR to any pub/sub component

## Configuration

```shell

dotnet add package AICentral.Dapr.Broadcast

```

```csharp

builder.Services.AddAICentral(
    builder.Configuration,
    additionalComponentAssemblies:
    [
        typeof(AICentral.Dapr.Broadcast.DaprBroadcaster).Assembly,
    ]);

```

```json

{
  "AICentral": {
    "GenericSteps": [
      {
        "Type": "DaprBroadcaster",
        "Name": "dapr-broadcaster",
        "Properties": {
          "DaprUri": "<dapr-listen-uri> e.g. https://localhost:62684/",
          "DaprProtocol": "Http|Grpc",
          "DaprToken": "Optional if you have a token",
          "DaprPubSubComponentName": "PubSubComponentName",
          "PubSubTopicName" : "PubSubTopicName"
        }
      }
    ]    
  }
}

```

## How does it work?

For every request handled by AI Central we publish a message to your DAPR pub-sub component with this information:

```json
{
  "data": {
    "callType": "Chat",
    "client": "",
    "completionTokens": null,
    "deploymentName": "gpt-4o",
    "duration": "00:00:00.5191121",
    "estimatedCompletionTokens": 236,
    "estimatedPromptTokens": 31,
    "id": "17ae68fa-bb51-462b-9917-9a7339d5b3ff",
    "internalEndpointName": "grfazopenai.openai.azure.com",
    "modelName": "gpt-4o-2024-05-13",
    "openAIHost": "endpoint1",
    "prompt": "system:\\n   text: You are a helpful assistant. You will replay with two or more paragraphs.\\n\\nuser:\\n   text: What is Azure OpenAI?\\n",
    "promptTokens": null,
    "remoteIpAddress": "::1",
    "response": "Response",
    "startDate": "2024-09-30T10:45:20.919928+08:00",
    "streamingResponse": true,
    "success": true,
    "totalTokens": 267
  }
}

```


