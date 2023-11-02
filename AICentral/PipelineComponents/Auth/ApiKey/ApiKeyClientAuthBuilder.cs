using AICentral.Configuration.JSON;
using AICentral.PipelineComponents.Endpoints.OpenAI;

namespace AICentral.PipelineComponents.Auth.ApiKey;

public class ApiKeyClientAuthBuilder : IAICentralClientAuthBuilder
{
    private readonly string _headerName;
    private readonly ConfigurationTypes.ApiKeyClientAuthClientConfig[] _clients;
    private readonly string _policyId = Guid.NewGuid().ToString();

    public ApiKeyClientAuthBuilder(string headerName,
        ConfigurationTypes.ApiKeyClientAuthClientConfig[] clients)
    {
        _headerName = headerName;
        _clients = clients;
    }

    public void RegisterServices(IServiceCollection services)
    {
        var schemeName = $"AICentralApiKey_{_policyId}";
        services.AddAuthentication().AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(schemeName, options =>
        {
            options.Clients = _clients;
            options.HeaderName = _headerName;
        });

        services.AddAuthorizationBuilder().AddPolicy(_policyId,
            policyBuilder => policyBuilder
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(schemeName));
    }

    public IAICentralClientAuthStep Build()
    {
        return new ApiKeyClientAuthProvider(_policyId);
    }

    public static IAICentralClientAuthBuilder BuildFromConfig(
        IConfigurationSection configurationSection)
    {
        var properties = configurationSection.Get<ConfigurationTypes.ApiKeyClientAuthConfig>()!;

        return new ApiKeyClientAuthBuilder(
            Guard.NotNull(properties.HeaderName, configurationSection, nameof(properties.HeaderName)),
            properties.Clients!.Length == 0
                ? throw new ArgumentException($"You must provide Clients in {configurationSection.Path}")
                : properties.Clients);
    }

    public static string ConfigName => "ApiKey";
}