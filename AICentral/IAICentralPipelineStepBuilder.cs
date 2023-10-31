namespace AICentral;

/// <summary>
/// Used to build pipeline steps that form the basis of AI Central's Pipelines.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IAICentralPipelineStepBuilder<out T> where T: IAICentralPipelineStep
{
    static virtual string ConfigName  => throw new NotImplementedException();

    static virtual IAICentralPipelineStepBuilder<T> BuildFromConfig(
        Dictionary<string, string> parameters) => throw new NotImplementedException();

    void RegisterServices(IServiceCollection services);

    T Build();

}