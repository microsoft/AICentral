namespace AICentral.Core;

public interface IAICentralGenericStepFactory<out T>: IAICentralPipelineStepFactory<T> where T : IAICentralPipelineStep
{
    static virtual IAICentralGenericStepFactory<T> BuildFromConfig(ILogger logger, IConfigurationSection section) => throw new NotImplementedException();

}