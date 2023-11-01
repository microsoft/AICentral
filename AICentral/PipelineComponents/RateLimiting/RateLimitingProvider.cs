using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace AICentral.PipelineComponents.RateLimiting;

public class RateLimitingProvider : IAICentralGenericStepBuilder<IAICentralPipelineStep>, IAICentralPipelineStep
{
    private readonly int _requestsPerWindow;
    private readonly string _id;
    private readonly int _windowTime;

    public RateLimitingProvider(int windowTime, int requestsPerWindow)
    {
        _windowTime = windowTime;
        _requestsPerWindow = requestsPerWindow;
        _id = Guid.NewGuid().ToString();
    }

    public void RegisterServices(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddFixedWindowLimiter(_id, window =>
            {
                window.Window = TimeSpan.FromSeconds(_windowTime);
                window.PermitLimit = _requestsPerWindow;
                window.QueueLimit = 0; //for now respond with 429 immediately if we've hit the limit. If this is set to > 0, then the middleware will hold the request waiting for the window to end.  
                window.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
            });
        });
    }

    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        app.UseRateLimiter();
        route.RequireRateLimiting(_id);
    }

    public static string ConfigName => "LocalRateLimiting";

    public static IAICentralGenericStepBuilder<IAICentralPipelineStep> BuildFromConfig(Dictionary<string, string> parameters)
    {
        return new RateLimitingProvider(
            parameters.TryGetValue("WindowTime", out var window)
                ? int.Parse(window)
                : throw new ArgumentException("Rate Limiting requires a WindowTime parameter"),
            parameters.TryGetValue("RequestsPerWindow", out var rpw)
                ? int.Parse(rpw)
                : throw new ArgumentException("Rate Limiting requires a RequestsPerWindow parameter")
        );
    }

    public IAICentralPipelineStep Build()
    {
        return this;
    }

    public Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
    {
        return pipeline.Next(context, cancellationToken);
    }

    public object WriteDebug()
    {
        return new
        {
            RequestsPerWindow = _requestsPerWindow,
            WindowTime = _windowTime
        };
    }
}

