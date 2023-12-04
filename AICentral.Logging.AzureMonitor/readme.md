# Azure Monitor Logger for use in AICentral

## To use, add a config section like below:

```json
{
  "GenericSteps": [
    {
      "Type": "AspNetCoreFixedWindowRateLimiting",
      "Name": "window-rate-limiter",
      "Properties": {
        "Window": "00:00:10",
        "PermitLimit": 5
      }
    },
    {
      "Type": "AzureMonitorLogger",
      "Name": "azure-monitor-logger",
      "Properties": {
        "WorkspaceId": "<workspace-id>",
        "Key": "<workspace-key>",
        "LogPrompt": true,
        "LogResponse": false
      }
    }
  ]
}
```

```csharp

builder.Services.AddAICentral(
    builder.Configuration,
    additionalComponentAssemblies: typeof(AzureMonitorLoggerFactory).Assembly);

```
