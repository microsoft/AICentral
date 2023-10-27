using AICentral.Pipelines;

namespace AICentral.Configuration;

public class AICentral
{
    private readonly bool _exposeTestPage;
    private readonly AICentralPipeline[] _pipelines;

    public AICentral(AICentralOptions options)
    {
        _exposeTestPage = options.ExposeTestPage;
        _pipelines = options.Pipelines.ToArray();
    }

    public void MapRoutes(WebApplication webApplication, ILogger<AICentral> logger)
    {
        foreach (var pipeline in _pipelines)
        {
            pipeline.MapRoutes(webApplication, logger);
        }

        if (_exposeTestPage)
        {
            logger.LogInformation("Exposing test page");
            webApplication.MapRazorPages();
        }
    }

    public void AddServices(IServiceCollection services)
    {
        foreach (var pipeline in _pipelines)
        {
            pipeline.AddServices(services);
        }

        if (_exposeTestPage)
        {
            services.AddRazorPages();
        }
    }

    public object WriteDebug()
    {
        return new
        {
            Pipelines = _pipelines.Select(x => x.WriteDebug()),
        };
    }
}