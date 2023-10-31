using AICentral.PipelineComponents.Endpoints;

namespace AICentral.PipelineComponents.EndpointSelectors;

public interface IAICentralEndpointSelectorBuilder
{
    static virtual string ConfigName  => throw new NotImplementedException();

    static virtual IAICentralEndpointSelectorBuilder BuildFromConfig(
        Dictionary<string, string> parameters,
        Dictionary<string, IAiCentralEndpointDispatcherBuilder> aiCentralEndpoints) => throw new NotImplementedException();

    IAICentralEndpointSelector Build(Dictionary<IAiCentralEndpointDispatcherBuilder, IAICentralEndpointDispatcher> builtEndpointDictionary);

    void RegisterServices(IServiceCollection services);
}