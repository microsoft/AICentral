namespace AICentral;

public class AICentralPipelines
{
    private readonly AICentralPipeline[] _pipelines;

    public AICentralPipelines(AICentralPipeline[] pipelines)
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