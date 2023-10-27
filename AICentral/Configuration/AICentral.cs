using AICentral.Pipelines;
using Newtonsoft.Json.Linq;

namespace AICentral.Configuration;

public class AICentral
{
    private readonly AICentralPipeline[] _pipelines;

    public AICentral(AICentralOptions options)
    {
        _pipelines = options.Pipelines.ToArray();
    }

    public void MapRoutes(WebApplication webApplication, ILogger<AICentral> logger)
    {
        foreach (var pipeline in _pipelines)
        {
            pipeline.MapRoutes(webApplication, logger);
        }
    }

    public void AddServices(IServiceCollection services)
    {
        foreach (var pipeline in _pipelines)
        {
            pipeline.AddServices(services);
        }
    }

    public JObject WriteDebug()
    {
        return JObject.FromObject(new
        {
            Pipelines = _pipelines.Select(x => x.WriteDebug()),
        });
    }
}