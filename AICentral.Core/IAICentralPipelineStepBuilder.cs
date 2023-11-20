namespace AICentral.Core;

/// <summary>
/// Used to build pipeline steps that form the basis of AI Central's Pipelines.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IAICentralPipelineStepBuilder<out T> where T: IAICentralPipelineStep
{
    static virtual string ConfigName  => throw new NotImplementedException();

    void RegisterServices(IServiceCollection services);

    T Build();

}