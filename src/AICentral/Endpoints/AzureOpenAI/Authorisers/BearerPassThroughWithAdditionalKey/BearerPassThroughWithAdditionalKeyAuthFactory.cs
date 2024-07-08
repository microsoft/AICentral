using AICentral.Core;

namespace AICentral.Endpoints.AzureOpenAI.Authorisers.BearerPassThroughWithAdditionalKey;

public class BearerPassThroughWithAdditionalKeyAuthFactory: IEndpointAuthorisationHandlerFactory
{
    private readonly BearerPassThroughWithAdditionalKeyAuth _provider;
    private BearerPassThroughWithAdditionalKeyAuthFactoryConfig _config;

    public BearerPassThroughWithAdditionalKeyAuthFactory(BearerPassThroughWithAdditionalKeyAuthFactoryConfig config)
    {
        _config = config;
        _provider = new BearerPassThroughWithAdditionalKeyAuth(_config);
    }
    
    public static string ConfigName => "BearerPlusKey";

    public static IEndpointAuthorisationHandlerFactory BuildFromConfig(ILogger logger, TypeAndNameConfig config)
    {
        var typed = config.TypedProperties<BearerPassThroughWithAdditionalKeyAuthFactoryConfig>();
        return new BearerPassThroughWithAdditionalKeyAuthFactory(typed);
    }

    public IEndpointAuthorisationHandler Build()
    {
        return _provider;
    }

    public object WriteDebug()
    {
        return new
        {
            Type = "BearerPlusKey",
            IncomingClaim = _config.IncomingClaimName,
            BackendHeader = _config.KeyHeaderName,
            MatchedUsers = _config.ClaimsToKeys!.Select(x => x.ClaimValue!.Substring(0, Math.Min(x.ClaimValue!.Length, 4)))
        };
    }
}