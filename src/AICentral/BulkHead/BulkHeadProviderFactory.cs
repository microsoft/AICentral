using AICentral.Core;

namespace AICentral.BulkHead;

public class BulkHeadProviderFactory : IPipelineStepFactory
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

    public IPipelineStep Build(IServiceProvider serviceProvider)
    {
        return _provider.Value;
    }

    public static string ConfigName => "BulkHead";

    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var properties = config.TypedProperties<BulkHeadConfiguration>()!;
        Guard.NotNull(properties.MaxConcurrency, nameof(properties.MaxConcurrency));

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