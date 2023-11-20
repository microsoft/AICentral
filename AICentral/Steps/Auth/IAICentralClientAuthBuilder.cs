using AICentral.Core;

namespace AICentral.Steps.Auth;

public interface IAICentralClientAuthBuilder : IAICentralPipelineStepBuilder<IAICentralClientAuthStep>
{
    static virtual IAICentralClientAuthBuilder BuildFromConfig(ILogger logger, IConfigurationSection configurationSection)
    {
        throw new NotImplementedException();
    }
}