namespace AICentral.Core;

public interface IAICentralGenericStepBuilder<out T>: IAICentralPipelineStepBuilder<T> where T : IAICentralPipelineStep
{
    static virtual IAICentralGenericStepBuilder<T> BuildFromConfig(ILogger logger, IConfigurationSection section) => throw new NotImplementedException();

}