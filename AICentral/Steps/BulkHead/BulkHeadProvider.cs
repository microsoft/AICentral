using AICentral.Core;

namespace AICentral.Steps.BulkHead;

public class BulkHeadProvider : IAICentralPipelineStep
{
    private readonly BulkHeadConfiguration _properties;
    private readonly SemaphoreSlim _semaphore;

    public BulkHeadProvider(BulkHeadConfiguration properties)
    {
        _properties = properties;
        _semaphore = new SemaphoreSlim(_properties.MaxConcurrency!.Value);
    }

    public async Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            return await pipeline.Next(context, aiCallInformation, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "FixedWindowRateLimiter",
            Properties = _properties
        };
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }
}

public class BulkHeadProviderBuilder : IAICentralGenericStepBuilder<IAICentralPipelineStep>
{
    private readonly BulkHeadConfiguration _properties;

    public BulkHeadProviderBuilder(BulkHeadConfiguration properties)
    {
        _properties = properties;
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public IAICentralPipelineStep Build()
    {
        return new BulkHeadProvider(_properties);
    }

    public static string ConfigName => "BulkHead";

    public static IAICentralGenericStepBuilder<IAICentralPipelineStep> BuildFromConfig(ILogger logger,
        IConfigurationSection section)
    {
        var properties = section.GetSection("Properties").Get<BulkHeadConfiguration>()!;
        Guard.NotNull(properties, section, "Properties");
        Guard.NotNull(properties.MaxConcurrency, section, nameof(properties.MaxConcurrency));

        return new BulkHeadProviderBuilder(properties);
    }
}