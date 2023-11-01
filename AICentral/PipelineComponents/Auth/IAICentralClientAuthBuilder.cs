namespace AICentral.PipelineComponents.Auth;

public interface IAICentralClientAuthBuilder : IAICentralPipelineStepBuilder<IAICentralClientAuthStep>
{
    static virtual IAICentralClientAuthBuilder BuildFromConfig(IConfigurationSection configurationSection, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }
    
    IAICentralClientAuthStep Build();

}