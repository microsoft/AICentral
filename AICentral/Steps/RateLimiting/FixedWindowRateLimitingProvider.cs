using System.Threading.RateLimiting;
using AICentral.Core;
using Microsoft.AspNetCore.RateLimiting;

namespace AICentral.Steps.RateLimiting;

public class FixedWindowRateLimitingProvider : IAICentralGenericStepFactory, IAICentralPipelineStep
{
    private readonly AICentralFixedWindowRateLimiterOptions _fixedWindowRateLimiterOptions;
    private readonly string _id;

    public FixedWindowRateLimitingProvider(AICentralFixedWindowRateLimiterOptions fixedWindowRateLimiterOptions)
    {
        _fixedWindowRateLimiterOptions = fixedWindowRateLimiterOptions;
        _id = Guid.NewGuid().ToString();
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            _fixedWindowRateLimiterOptions.Options!.AutoReplenishment = false;
            options.AddPolicy(_id,
                ctx => RateLimitPartition.GetFixedWindowLimiter(GetPartitionId(ctx),
                    _ => _fixedWindowRateLimiterOptions.Options));
        });
    }

    private string GetPartitionId(HttpContext context)
    {
        var id = _fixedWindowRateLimiterOptions.LimitType == FixedWindowRateLimitingLimitType.PerAICentralEndpoint
            ? "__endpoint"
            : context.User.Identity?.Name ?? "unknown";
        return id;
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        app.UseRateLimiter();
        route.RequireRateLimiting(_id);
    }

    public static string ConfigName => "AspNetCoreFixedWindowRateLimiting";

    public static IAICentralGenericStepFactory BuildFromConfig(
        ILogger logger,
        IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties").Get<AICentralFixedWindowRateLimiterOptions>()!;
        Guard.NotNull(properties, configurationSection, "Properties");
        Guard.NotNull(properties.LimitType, configurationSection, nameof(properties.LimitType));
        Guard.NotNull(properties.Options, configurationSection, nameof(properties.Options));

        return new FixedWindowRateLimitingProvider(properties);
    }

    public IAICentralPipelineStep Build()
    {
        return this;
    }

    public Task<AICentralResponse> Handle(HttpContext context, AICallInformation aiCallInformation,
        IAICentralPipelineExecutor pipeline,
        CancellationToken cancellationToken)
    {
        return pipeline.Next(context, aiCallInformation, cancellationToken);
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "FixedWindowRateLimiter",
            Properties = _fixedWindowRateLimiterOptions
        };
    }
}