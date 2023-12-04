using AICentral.Configuration.JSON;
using AICentral.Core;

namespace AICentral.Steps.Auth.ApiKey;

public class ApiKeyClientAuthFactory : IAICentralClientAuthFactory
{
    private readonly ConfigurationTypes.ApiKeyClientAuthConfig _config;
    private readonly string _policyId = Guid.NewGuid().ToString();
    private readonly Lazy<ApiKeyClientAuthProvider> _singleton;

    public ApiKeyClientAuthFactory(ConfigurationTypes.ApiKeyClientAuthConfig config)
    {
        _config = config;
        _singleton = new Lazy<ApiKeyClientAuthProvider>(() => new ApiKeyClientAuthProvider());
    }

    public void RegisterServices(IServiceCollection services)
    {
        var schemeName = $"AICentralApiKey_{_policyId}";
        services.AddAuthentication().AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(schemeName, options =>
        {
            options.Clients = _config.Clients!;
            options.HeaderName = "api-key";
        });

        services.AddAuthorizationBuilder().AddPolicy(_policyId,
            policyBuilder => policyBuilder
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(schemeName));
    }

    public IAICentralClientAuthStep Build()
    {
        return _singleton.Value;
    }

    public static IAICentralClientAuthFactory BuildFromConfig(
        ILogger logger, 
        IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.ApiKeyClientAuthConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");

        return new ApiKeyClientAuthFactory(
            properties!.Clients!.Length == 0
                ? throw new ArgumentException($"You must provide Clients in {configurationSection.Path}")
                : properties);
    }

    public static string ConfigName => "ApiKey";

    public object WriteDebug()
    {
        return new
        {
            Type = "Client Api Key",
            Clients = _config.Clients!.Select(x => new
            {
                ClientName = x.ClientName,
                Key1 = x.Key1!.Substring(0,1) + "****" + x.Key1[^1],
                Key2 = x.Key2!.Substring(0,1) + "****" + x.Key1[^1],
            })
        };
    }
        
    public void ConfigureRoute(WebApplication app, IEndpointConventionBuilder route)
    {
        route.RequireAuthorization(_policyId);
    }
}