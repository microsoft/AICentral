# AI Central Core

This package contains the Core Interfaces for building your own Extensibility into AI Central.

See [https://github.com/microsoft/AICentral/tree/main/AICentral.Logging.AzureMonitor](https://github.com/microsoft/AICentral/tree/main/AICentral.Logging.AzureMonitor) for an example.

## Structure
An extensibility project requires 3 classes:

### Config
A simple class that contains the configuration for your extension.

### Factory

A class that can determine how to create an instance of the extension given your configuration.

It must implement from ```AICentral.Core.IAICentralGenericStepBuilder<IAICentralPipelineStep>```

It must also override the 2 static methods on the above interface:

```csharp

    public static string ConfigName => "<name-to-reference-the-step-in-config>";

    public static IAICentralGenericStepFactory<IAICentralPipelineStep> BuildFromConfig(
        ILogger logger, 
        AICentralTypeAndNameConfig config)
    {
        // build a factory implementation that can provide instances (or a singleton if you prefer) of your extension. 
    }
```

### Extension
A class that provides the extension functionality, by implementing the ```AICentral.Core.IAICentralPipelineStep```.
