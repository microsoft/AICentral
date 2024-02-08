using AICentral.Core;

namespace AICentral.Affinity;

public class SingleNodeAffinityFactory : IPipelineStepFactory
{
    public static string ConfigName => "SingleNodeAffinity";
    private readonly SingleNodeAffinity _provider;

    public SingleNodeAffinityFactory(TimeSpan slidingWindow)
    {
        _provider = new SingleNodeAffinity(slidingWindow);
    }

    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var props = config.TypedProperties<SingleNodeAffinityConfig>();
        return new SingleNodeAffinityFactory(
            Guard.NotNull(props.SlidingAffinityWindow, nameof(props.SlidingAffinityWindow))!.Value
        );
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public IPipelineStep Build(IServiceProvider serviceProvider)
    {
        return _provider;
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "SingleNodeAffinity"
        };
    }
}