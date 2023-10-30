namespace AICentral.Pipelines.Auth;

public interface IAICentralClientAuthProvider : IAICentralPipelineStep<IAICentralClientAuth>
{
    static virtual IAICentralClientAuthProvider BuildFromConfig(IConfigurationSection configurationSection, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }
}

public interface IAICentralClientAuth : IAICentralPipelineStepRuntime
{
}