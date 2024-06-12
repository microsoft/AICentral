using AICentral.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AICentral.RateLimiting.DistributedRedis;

public class DistributedRateLimiterFactory : IPipelineStepFactory
{
    private readonly string _redisConfiguration;
    private readonly TimeSpan _window;
    private readonly int _limit;
    private readonly MetricType _metricType;
    private readonly LimitType _limitType;
    private readonly string _id;
    private string _stepName;

    private DistributedRateLimiterFactory(
        string stepName,
        string redisConfiguration,
        TimeSpan window, int limit,
        MetricType metricType,
        LimitType limitType)
    {
        _id = Guid.NewGuid().ToString();
        _stepName = stepName;
        _redisConfiguration = redisConfiguration;
        _window = window;
        _limit = limit;
        _metricType = metricType;
        _limitType = limitType;
    }

    IPipelineStep IPipelineStepFactory.Build(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredKeyedService<DistributedRateLimiter>(_id);
    }

    public static string ConfigName => "DistributedRateLimiter";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddKeyedSingleton(_id, ConnectionMultiplexer.Connect(_redisConfiguration));
        
        services.AddKeyedTransient<DistributedRateLimiter>(_id, (sp, key) => new DistributedRateLimiter(
            _stepName,
            sp.GetRequiredKeyedService<ConnectionMultiplexer>(_id).GetDatabase(),
            _window,
            _limit,
            _limitType,
            _metricType
        ));
    }

    public static IPipelineStepFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var typedConfig = config.TypedProperties<DistributedRateLimiterConfig>();
        return new DistributedRateLimiterFactory(
            config.Name!,
            Guard.NotNull(typedConfig.RedisConfiguration, nameof(typedConfig.RedisConfiguration)),
            Guard.NotNull(typedConfig.Window, nameof(typedConfig.Window))!.Value,
            Guard.NotNull(typedConfig.PermitLimit, nameof(typedConfig.PermitLimit))!.Value,
            Guard.NotNull(typedConfig.MetricType, nameof(typedConfig.MetricType))!.Value,
            Guard.NotNull(typedConfig.LimitType, nameof(typedConfig.LimitType))!.Value
        );
    }

    public object WriteDebug()
    {
        return new
        {
            Window = _window,
            CounterType = _metricType,
            LimitType = _limitType,
            Limit = _limit
        };
    }

    public void ConfigureRoute(WebApplication webApplication, IEndpointConventionBuilder route)
    {
    }
}