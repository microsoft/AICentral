namespace AICentral.Core;

/// <summary>
/// Used to build classes that dispatch requests to the correct endpoint, or to an endpoint selector (which will then dispatch onwards).
/// </summary>
/// <remarks>
/// This interface is in the Core library to support Endpoint Selectors that need to dispatch to other Endpoint Selectors.
/// Normal usage does not require you to provide your own implementation of this interface.  
/// </remarks>
public interface IAICentralEndpointDispatcherFactory
{
    IAICentralEndpointDispatcher Build();
    object WriteDebug();
    void RegisterServices(HttpMessageHandler? optionalHandler, IServiceCollection services);
}