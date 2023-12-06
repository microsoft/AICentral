using AICentral.Core;

namespace AICentral.Steps.BulkHead;

public class BulkHeadProviderFactory : IAICentralGenericStepFactory
{
    private readonly BulkHeadConfiguration _properties;
    private readonly Lazy<BulkHeadProvider> _provider;

    public BulkHeadProviderFactory(BulkHeadConfiguration properties)
    {
        _properties = properties;
        _provider = new Lazy<BulkHeadProvider>(() => new BulkHeadProvider(_properties));
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public IAICentralPipelineStep Build()
    {
        return _provider.Value;
    }

    public static string ConfigName => "BulkHead";

    public static IAICentralGenericStepFactory BuildFromConfig(ILogger logger,
        IConfigurationSection section)
    {
        var properties = section.GetSection("Properties").Get<BulkHeadConfiguration>()!;
        Guard.NotNull(properties, section, "Properties");
        Guard.NotNull(properties.MaxConcurrency, section, nameof(properties.MaxConcurrency));

        return new BulkHeadProviderFactory(properties);
    }
    
    public object WriteDebug()
    {
        return new
        {
            Type = "BulkHead",
            Properties = _properties
        };
    }

    public void ConfigureRoute(WebApplication webApplication, IEndpointConventionBuilder route)
    {
    }
}