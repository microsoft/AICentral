namespace AICentral.Core;

public interface IEndpointRequestResponseHandlerFactory
{
    static virtual string ConfigName  => throw new NotImplementedException();

    void RegisterServices(
        HttpMessageHandler? httpMessageHandler, 
        IServiceCollection services);

    static virtual IEndpointRequestResponseHandlerFactory BuildFromConfig(ILogger logger, AICentralTypeAndNameConfig config)
    {
        throw new NotImplementedException();
    }

    IEndpointRequestResponseHandler Build();

    object WriteDebug();
}