using AICentral.Core;

namespace AICentral.RequestFiltering;

public class RequestFilteringProviderFactory : IPipelineStepFactory
{
    private readonly RequestFilteringConfiguration _properties;
    private readonly Lazy<RequestFilteringProvider> _provider;

    public RequestFilteringProviderFactory(RequestFilteringConfiguration properties)
    {
        _properties = properties;
        _provider = new Lazy<RequestFilteringProvider>(() => new RequestFilteringProvider(_properties));
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public IPipelineStep Build(IServiceProvider serviceProvider)
    {
        return _provider.Value;
    }

    public static string ConfigName => "RequestFiltering";

    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var properties = config.TypedProperties<RequestFilteringConfiguration>()!;
        Guard.NotNull(properties.AllowedHostNames, nameof(properties.AllowedHostNames));

        return new RequestFilteringProviderFactory(properties);
    }
    
    public object WriteDebug()
    {
        return new
        {
            Type = "RequestFilter",
            Properties = _properties
        };
    }

    public void ConfigureRoute(WebApplication webApplication, IEndpointConventionBuilder route)
    {
    }
}