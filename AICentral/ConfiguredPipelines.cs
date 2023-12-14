namespace AICentral;

public class ConfiguredPipelines
{
    private readonly Pipeline[] _pipelines;

    public ConfiguredPipelines(Pipeline[] pipelines)
    {
        _pipelines = pipelines;
    }

    public void BuildRoutes(WebApplication webApplication)
    {
        foreach (var pipeline in _pipelines)
        {
            pipeline.BuildRoute(webApplication);
        }
    }

    public object[] WriteDebug()
    {
        return _pipelines.Select(x => x.WriteDebug()).ToArray();
    }
}