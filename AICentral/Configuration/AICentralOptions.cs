using AICentral.Pipelines;

namespace AICentral.Configuration;

public class AICentralOptions
{
    public IList<AICentralPipeline> Pipelines = new List<AICentralPipeline>();
}