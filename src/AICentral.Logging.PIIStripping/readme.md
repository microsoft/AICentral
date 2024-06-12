# AICentral.Logging.PIIStripping

An Asynchronous logger for use with (AICentral)[www.github.com/microsoft/aicentral].
This logger leverages the Azure Text Analytics API to strip prompts and responses of PII data.

## Configuration

```shell

dotnet add package AICentral.Logging.PIIStripping

```

```csharp

builder.Services.AddAICentral(
    builder.Configuration,
    additionalComponentAssemblies:
    [
        typeof(PIIStrippingLogger).Assembly,
    ]);

```

```json

{
  "AICentral": {
    "GenericSteps": [
      {
        "Type": "PIIStrippingLogger",
        "Name": "pii-stripping-logger",
        "Properties": {
          "StorageQueueConnectionString": "<storage-connection-string>",
          "QueueName": "queue-to-write-to",
          "TextAnalyticsEndpoint": "<text-analytics-uri>",
          "TextAnalyticsKey": "<text-analytics-key-if-use-default-credential-false>",
          "CosmosDatabase": "cosmos-database-to-log-to",
          "CosmosContainer": "container-to-log-to",
          "CosmosConnectionString": "<text-analytics-key-if-use-default-credential-false>"
        }
      }
    ]    
  }
}

```

## How does it work?

This logger is a wrapper around the AICentral logger that uses the Text Analytics API to strip PII data from the prompts and responses before logging them to Cosmos.
Log statements are initially written to an Azure Queue, and then read from the queue by a separate background service, that uses the Text Analytics API to strip PII data.
The stripped log statements are then written to Cosmos.

The Container needs to have a partition key specified by '/id'.
