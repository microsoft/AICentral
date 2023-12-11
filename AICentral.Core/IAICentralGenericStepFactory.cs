namespace AICentral.Core;

public interface IAICentralGenericStepFactory: IAICentralPipelineStepFactory<IAICentralPipelineStep>
{
    static virtual IAICentralGenericStepFactory BuildFromConfig(ILogger logger, AICentralTypeAndNameConfig config) => throw new NotImplementedException();
    object WriteDebug();
    void ConfigureRoute(WebApplication webApplication, IEndpointConventionBuilder route);
}