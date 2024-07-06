using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI.Authorisers.BearerPassThroughWithAdditionalKey;

public class BearerPassThroughWithAdditionalKeyAuthFactory: IEndpointAuthorisationHandlerFactory
{
    private readonly BearerPassThroughWithAdditionalKeyAuth _provider;

    public BearerPassThroughWithAdditionalKeyAuthFactory(BearerPassThroughWithAdditionalKeyAuthFactoryConfig config)
    {
        _provider = new BearerPassThroughWithAdditionalKeyAuth(config);
    }
    
    public static string ConfigName => "BearerPlusKey";

    public static IEndpointAuthorisationHandlerFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var typed = config.TypedProperties<BearerPassThroughWithAdditionalKeyAuthFactoryConfig>();
        return new BearerPassThroughWithAdditionalKeyAuthFactory(typed);
    }

    public void RegisterServices(IServiceCollection services)
    {
    }

    public IEndpointAuthorisationHandler Build()
    {
        return _provider;
    }

    public object WriteDebug()
    {
        throw new NotImplementedException();
    }
}