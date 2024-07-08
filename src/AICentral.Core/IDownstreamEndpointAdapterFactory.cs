namespace AICentral.Core;

public interface IDownstreamEndpointAdapterFactory
{
    static virtual string ConfigName  => throw new NotImplementedException();

    void RegisterServices(
        HttpMessageHandler? httpMessageHandler, 
        IServiceCollection services);

    static virtual IDownstreamEndpointAdapterFactory BuildFromConfig(
        ILogger logger, 
        TypeAndNameConfig config,
        IDictionary<string, IEndpointAuthorisationHandlerFactory> authorisationHandlerFactories)
    {
        throw new NotImplementedException();
    }

    IDownstreamEndpointAdapter Build();

    object WriteDebug();
}