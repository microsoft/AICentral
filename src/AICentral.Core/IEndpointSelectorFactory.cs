namespace AICentral.Core;

/// <summary>
/// Factory class to build instances of IAICentralEndpointSelectors
/// </summary>
public interface IEndpointSelectorFactory
{
    /// <summary>
    /// Name representing this endpoint selector. Used in config files.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    static virtual string ConfigName  => throw new NotImplementedException();

    /// <summary>
    /// Builds an instance of the factory from a config file.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="config"></param>
    /// <param name="aiCentralEndpoints"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    static virtual IEndpointSelectorFactory BuildFromConfig(
        ILogger logger, 
        TypeAndNameConfig config,
        Dictionary<string, IEndpointDispatcherFactory> aiCentralEndpoints
        ) => throw new NotImplementedException();

    /// <summary>
    /// Builds an instance of the selector. 
    /// </summary>
    /// <returns></returns>
    IEndpointSelector Build();

    /// <summary>
    /// Register any required services in the DI container.
    /// </summary>
    /// <param name="services"></param>
    void RegisterServices(IServiceCollection services);

    /// <summary>
    /// Return an object with debug information about the endpoint selector
    /// </summary>
    /// <returns></returns>
    object WriteDebug();
}