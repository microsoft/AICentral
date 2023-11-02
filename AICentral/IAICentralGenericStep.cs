namespace AICentral;

public interface IAICentralGenericStepBuilder<out T>: IAICentralPipelineStepBuilder<T> where T : IAICentralPipelineStep
{
    static virtual IAICentralGenericStepBuilder<T> BuildFromConfig(IConfigurationSection section) => throw new NotImplementedException();

}