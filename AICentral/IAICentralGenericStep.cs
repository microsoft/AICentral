namespace AICentral;

public interface IAICentralGenericStepBuilder<out T>: IAICentralPipelineStepBuilder<T> where T : IAICentralPipelineStep
{
    static virtual IAICentralGenericStepBuilder<T> BuildFromConfig(
        Dictionary<string, string> parameters) => throw new NotImplementedException();

}