using AICentral.Pipelines;

namespace AICentral.Configuration;

public class AICentralOptions
{
    public IList<AICentralPipeline> Pipelines = new List<AICentralPipeline>();
    public bool ExposeTestPage { get; set; }

    public void AddServices(IServiceCollection services)
    {
        foreach (var pipeline in Pipelines)
        {
            pipeline.AddServices(services);
        }

        if (ExposeTestPage)
        {
            services.AddRazorPages();
        }
    }
}