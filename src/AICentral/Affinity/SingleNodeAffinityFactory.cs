using AICentral.Core;

namespace AICentral.Affinity;

public class SingleNodeAffinityFactory : IPipelineStepFactory
{
    public static string ConfigName => "SingleNodeAffinity";
    private readonly Affinity _provider;

    public SingleNodeAffinityFactory(TimeSpan slidingWindow)
    {
        _provider = new Affinity(slidingWindow);
    }

    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var props = config.TypedProperties<AffinityConfig>();
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