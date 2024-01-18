using AICentral.Core;

namespace AICentral.ConsumerAuth;

public interface IConsumerAuthFactory : IAICentralPipelineStepFactory<IConsumerAuthStep>
{
    static virtual IConsumerAuthFactory BuildFromConfig(ILogger logger, AICentralTypeAndNameConfig config)
    {
        throw new NotImplementedException();
    }

    object WriteDebug();

    void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route);
}