using AICentral.Pipelines.Endpoints;

namespace AICentral.Pipelines.EndpointSelectors;

public interface IAICentralEndpointSelector
{
    static virtual string ConfigName  => throw new NotImplementedException();

    static virtual IAICentralEndpointSelector BuildFromConfig(
        Dictionary<string, string> parameters,
        Dictionary<string, IAICentralEndpoint> aiCentralEndpoints) => throw new NotImplementedException();

    IAICentralEndpointSelectorRuntime Build(Dictionary<IAICentralEndpoint, IAICentralEndpointRuntime> builtEndpointDictionary);

    void RegisterServices(IServiceCollection services);

    void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route);
}

public interface IAICentralEndpointSelectorRuntime : IAICentralPipelineStepRuntime
{
}
