namespace AICentral.Core;

public interface IDownstreamEndpointAdapter
{
    static virtual string ConfigName  => throw new NotImplementedException();

    void RegisterServices(
        HttpMessageHandler? httpMessageHandler, 
        IServiceCollection services);

    static virtual IDownstreamEndpointAdapter BuildFromConfig(ILogger logger, AICentralTypeAndNameConfig config)
    {
        throw new NotImplementedException();
    }

    IEndpointAdapter Build();

    object WriteDebug();
}