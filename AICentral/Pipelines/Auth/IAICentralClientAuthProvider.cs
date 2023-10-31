namespace AICentral.Pipelines.Auth;

public interface IAICentralClientAuthProvider : IAICentralPipelineStep<IAICentralClientAuthRuntime>
{
    static virtual IAICentralClientAuthProvider BuildFromConfig(IConfigurationSection configurationSection, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }
    
    IAICentralClientAuthRuntime Build();

}

public interface IAICentralClientAuthRuntime : IAICentralPipelineStepRuntime
{
}