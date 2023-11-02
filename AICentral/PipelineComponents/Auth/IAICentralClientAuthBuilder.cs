namespace AICentral.PipelineComponents.Auth;

public interface IAICentralClientAuthBuilder : IAICentralPipelineStepBuilder<IAICentralClientAuthStep>
{
    static virtual IAICentralClientAuthBuilder BuildFromConfig(IConfigurationSection configurationSection)
    {
        throw new NotImplementedException();
    }
}