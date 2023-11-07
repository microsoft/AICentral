namespace AICentral.Steps.Auth;

public interface IAICentralClientAuthBuilder : IAICentralPipelineStepBuilder<IAICentralClientAuthStep>
{
    static virtual IAICentralClientAuthBuilder BuildFromConfig(IConfigurationSection configurationSection)
    {
        throw new NotImplementedException();
    }
}