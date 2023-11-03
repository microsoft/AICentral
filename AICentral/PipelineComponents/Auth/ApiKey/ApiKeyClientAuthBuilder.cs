using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Endpoints.OpenAI;

namespace AICentral.PipelineComponents.Auth.ApiKey;

public class ApiKeyClientAuthBuilder : IAICentralClientAuthBuilder
{
    private readonly ConfigurationTypes.ApiKeyClientAuthConfig _config;
    private readonly string _policyId = Guid.NewGuid().ToString();

    public ApiKeyClientAuthBuilder(ConfigurationTypes.ApiKeyClientAuthConfig config)
    {
        _config = config;
    }

    public void RegisterServices(IServiceCollection services)
    {
        var schemeName = $"AICentralApiKey_{_policyId}";
        services.AddAuthentication().AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(schemeName, options =>
        {
            options.Clients = _config.Clients!;
            options.HeaderName = _config.HeaderName!;
        });

        services.AddAuthorizationBuilder().AddPolicy(_policyId,
            policyBuilder => policyBuilder
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(schemeName));
    }

    public IAICentralClientAuthStep Build()
    {
        return new ApiKeyClientAuthProvider(_policyId, _config);
    }

    public static IAICentralClientAuthBuilder BuildFromConfig(
        IConfigurationSection configurationSection)
    {
        var properties = configurationSection.GetSection("Properties").Get<ConfigurationTypes.ApiKeyClientAuthConfig>();
        Guard.NotNull(properties, configurationSection, "Properties");
        Guard.NotNull(properties!.HeaderName, configurationSection, nameof(properties.HeaderName));

        return new ApiKeyClientAuthBuilder(
            properties.Clients!.Length == 0
                ? throw new ArgumentException($"You must provide Clients in {configurationSection.Path}")
                : properties);
    }

    public static string ConfigName => "ApiKey";
}