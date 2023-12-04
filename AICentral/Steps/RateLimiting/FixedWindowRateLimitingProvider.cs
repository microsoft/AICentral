using System.Threading.RateLimiting;
using AICentral.Core;
using AICentral.Steps.EndpointSelectors;
using Microsoft.AspNetCore.RateLimiting;

namespace AICentral.Steps.RateLimiting;

public class FixedWindowRateLimitingProvider : IAICentralGenericStepFactory, IAICentralPipelineStep
{
    private readonly FixedWindowRateLimiterOptions _fixedWindowRateLimiterOptions;
    private readonly string _id;

    public FixedWindowRateLimitingProvider(FixedWindowRateLimiterOptions fixedWindowRateLimiterOptions)
    {
        _fixedWindowRateLimiterOptions = fixedWindowRateLimiterOptions;
        _id = Guid.NewGuid().ToString();
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddRateLimiter( options =>
        {
            options.RejectionStatusCode = 429;
            options.AddFixedWindowLimiter(_id, window =>
            {
                window.Window = _fixedWindowRateLimiterOptions.Window;
                window.PermitLimit = _fixedWindowRateLimiterOptions.PermitLimit;
                window.QueueLimit = _fixedWindowRateLimiterOptions.QueueLimit;
                window.QueueProcessingOrder = _fixedWindowRateLimiterOptions.QueueProcessingOrder;
                window.AutoReplenishment = _fixedWindowRateLimiterOptions.AutoReplenishment;
            });
        });
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
        var properties = configurationSection.GetSection("Properties").Get<FixedWindowRateLimiterOptions>()!;
        Guard.NotNull(properties, configurationSection, "Properties");

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